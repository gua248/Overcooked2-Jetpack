using AssetBundles;
using OC2Jetpack.Extension;
using UnityEngine;
using System;
using System.Collections.Generic;
using BitStream;
using Team17.Online.Multiplayer.Messaging;
using System.Linq;
using InControl;

namespace OC2Jetpack
{
    public class JetpackPlayerControl : MonoBehaviour
    {
        private const float sixtyFPS = 0.016666668f;
        public const float liftForce = 50;
        public const float maxSpeed = 5;
        public const float lowGravity = 20;
        public const float highGravity = 100;
        public static GameObject sprayEffectPrefab = null;
        public static int m_iFalling = Animator.StringToHash("Falling");
        public static Team17.Online.Multiplayer.Client localClient = null;
        public static Team17.Online.Multiplayer.Server localServer = null;
        public const MessageType jetpackMessageType = (MessageType)66;
        public static Dictionary<uint, JetpackPlayerControl> dictionaryChefEntityID = new Dictionary<uint, JetpackPlayerControl>();

        public JetpackButton jetpackButton;
        public ClientJetpackPlayerControl clientJetpackPlayerControl;
        public ServerJetpackPlayerControl serverJetpackPlayerControl;

        public PlayerAnimationDecisions playerAnimationDecisions;
        public ParticleSystem sprayEffect = null;
        
        public PlayerControls playerControls;
        public ClientPlayerControlsImpl_Default clientPlayerControlsImpl_Default;
        public bool isServer;
        public uint chefEntityID;

        public void Awake()
        {
            clientPlayerControlsImpl_Default = gameObject.RequireComponent<ClientPlayerControlsImpl_Default>();
            playerAnimationDecisions = gameObject.RequireComponent<PlayerAnimationDecisions>();
            playerControls = gameObject.RequireComponent<PlayerControls>();
            playerControls.Movement.GravityStrength = lowGravity;

            PlayerInputLookup.Player player = playerControls.PlayerIDProvider.GetID();
            PlayerGameInput playerInput = PlayerInputLookup.GetInputConfig().GetInputData(player);
            Key jetpackKey = playerInput == null ? Key.None : 
                (playerInput.Side == PadSide.Both ? JetpackKeyboardRebind.jetpackKey[0] : 
                (playerInput.Side == PadSide.Left ? JetpackKeyboardRebind.jetpackKey[1] :
                JetpackKeyboardRebind.jetpackKey[2]));
            jetpackButton = jetpackKey == Key.None ? null : new JetpackButton(jetpackKey.ToKeyCode());

            isServer = gameObject.GetComponent<ServerInputReceiver>() != null;
            clientJetpackPlayerControl = gameObject.AddComponent<ClientJetpackPlayerControl>();
            serverJetpackPlayerControl = isServer ? gameObject.AddComponent<ServerJetpackPlayerControl>() : null;
            chefEntityID = EntitySerialisationRegistry.GetEntry(gameObject).m_Header.m_uEntityID;
            dictionaryChefEntityID[chefEntityID] = this;
        }

        public void OnDestroy()
        {
            dictionaryChefEntityID.Remove(chefEntityID);
        }

        public static void OnMessageReceived(JetpackMessage message)
        {
            if (!dictionaryChefEntityID.ContainsKey(message.chefEntityID))
                return;
            JetpackPlayerControl player = dictionaryChefEntityID[message.chefEntityID];
            if (message.server2client)
                player.clientJetpackPlayerControl.OnMessageReceived(message.jetting, message.cruising);
            else
                player.serverJetpackPlayerControl.OnMessageReceived(message.jetting, message.cruising);
        }

        public static bool ApplyLiftForce(ClientPlayerControlsImpl_Default clientPlayerControlsImpl_Default)
        {
            // Prefix of ApplyGravityForce()
            EntitySerialisationEntry entry = EntitySerialisationRegistry.GetEntry(clientPlayerControlsImpl_Default.gameObject);
            uint chefEntityID = entry.m_Header.m_uEntityID;
            if (!dictionaryChefEntityID.ContainsKey(chefEntityID))
                return true;
            JetpackPlayerControl player = dictionaryChefEntityID[chefEntityID];
            if (!player.clientJetpackPlayerControl.jetting) return true;

            Vector3 v = player.playerControls.Motion.GetVelocity();
            v.y = Math.Min(v.y + JetpackPlayerControl.liftForce * sixtyFPS, JetpackPlayerControl.maxSpeed);
            if (player.clientJetpackPlayerControl.cruising && v.y > 0f)
            {
                v.y = 0f;
                player.playerControls.Motion.SetVelocity(v);
                return false;
            }
            player.playerControls.Motion.SetVelocity(v);
            return true;
        }

        public static void DisableCeiling()
        {
            GameObject ceiling = GameObject.Find("Ceiling");
            ceiling?.SetActive(false);
        }

        public static void AttachControl()
        {
            if (JetpackKeyboardRebind.jetpackKey.All(key => key == Key.None)) return;
            DisableCeiling();
            for (int i = 0; i < PlayerIDProvider.s_AllProviders.Count; i++)
            {
                PlayerIDProvider playerIDProvider = PlayerIDProvider.s_AllProviders._items[i];
                playerIDProvider.gameObject.AddComponent<JetpackPlayerControl>();
            }
        }

        public static void FindSprayEffectPrefab()
        {
            if (sprayEffectPrefab != null)
                return;
            LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle("bundle46", out string _);
            sprayEffectPrefab = (GameObject)bundle.m_AssetBundle.LoadAsset("assets/particles/overcooked_legacy/pfx_fireextinguishspray.prefab");
        }
    }

    public class ServerJetpackPlayerControl : MonoBehaviour
    {
        JetpackPlayerControl jetpackPlayerControl;

        public void Awake()
        {
            jetpackPlayerControl = GetComponent<JetpackPlayerControl>();
        }

        public void OnDestroy()
        {
            StopJet();
        }

        public void Update()
        {
            if (jetpackPlayerControl.clientJetpackPlayerControl.jetting)
            {
                FightFire();
                if (!jetpackPlayerControl.clientJetpackPlayerControl.cruising 
                    && !jetpackPlayerControl.playerControls.GetDirectlyUnderPlayerControl()
                    || !jetpackPlayerControl.playerControls.enabled)
                    StopJet();
            }
        }

        public void OnMessageReceived(bool messageJetting, bool messageCruising)
        {
            jetpackPlayerControl.playerControls.SetDirectlyUnderPlayerControl(true);
            if (!messageJetting) StopJet();
            else if (messageCruising) StartCruise();
            else StartJet();
        }

        public void StartJet()
        {
            if (jetpackPlayerControl.clientPlayerControlsImpl_Default.get_m_impactTimer() > 0f)
                return;
            SendServerJetpackMessage(new JetpackMessage(true, false, jetpackPlayerControl.chefEntityID, true));
        }

        public void StopJet()
        {
            SendServerJetpackMessage(new JetpackMessage(false, false, jetpackPlayerControl.chefEntityID, true)); ;
        }

        public void StartCruise()
        {
            SendServerJetpackMessage(new JetpackMessage(true, true, jetpackPlayerControl.chefEntityID, true));
        }

        private void SendServerJetpackMessage(JetpackMessage message)
        {
            if (MultiplayerController.IsSynchronisationActive())
                JetpackPlayerControl.localServer?.BroadcastMessageToAll(JetpackPlayerControl.jetpackMessageType, message, true);
        }

        private void FightFire()
        {
            List<ServerFlammable> list = new List<ServerFlammable>();
            foreach (ServerFlammable serverFlammable in ServerFlammable.GetAllOnFire())
            {
                if (IsInSpray(serverFlammable.transform))
                {
                    list.Add(serverFlammable);
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                ServerFlammable serverFlammable2 = list[i];
                serverFlammable2.FightFire(0.5f, TimeManager.GetDeltaTime(gameObject), true);
            }
        }

        private bool IsInSpray(Transform tFire)
        {
            Vector3 pPlayer = transform.position;
            Vector3 pFire = tFire.position;
            Vector3 lhs = pFire - pPlayer;
            float distance = 4f;
            float angle = 15f;
            float num = (distance + 0.6f) * (distance + 0.6f);
            if (lhs.sqrMagnitude >= num)
                return false;
            float num2 = Vector3.Dot(lhs, -transform.up);
            if (num2 <= 0f || num2 >= distance + 0.6f)
                return false;
            float num3 = num2 * Mathf.Sin(0.017453292f * angle * 0.5f);
            Vector3 b = transform.position - transform.up * num2;
            float num4 = (num3 + 0.6f) * (num3 + 0.6f);
            if (num4 > (pFire - b).sqrMagnitude)
                return true;
            return false;
        }
    }

    public class ClientJetpackPlayerControl : MonoBehaviour
    {
        JetpackPlayerControl jetpackPlayerControl;
        public bool jetting;
        public bool cruising;

        public void Awake()
        {
            jetpackPlayerControl = GetComponent<JetpackPlayerControl>();
        }

        public void Update()
        {
            if (jetpackPlayerControl.clientPlayerControlsImpl_Default.get_m_impactTimer() > 0f)
            {
                jetpackPlayerControl.playerControls.Movement.GravityStrength = JetpackPlayerControl.highGravity;
                if (!jetting) return;
                if (jetpackPlayerControl.isServer)
                    jetpackPlayerControl.serverJetpackPlayerControl.StopJet();
                else if (jetpackPlayerControl.jetpackButton != null)
                    StopJet();
                return;
            }
            jetpackPlayerControl.playerControls.Movement.GravityStrength = JetpackPlayerControl.lowGravity;

            if (jetpackPlayerControl.jetpackButton == null)
                return;

            if (TimeManager.IsPaused(TimeManager.PauseLayer.Main)
                || !jetpackPlayerControl.playerControls.CanButtonBePressed())
            {
                if (jetting && !cruising) StopJetAndMessage();
                return;
            }

            if (!MultiplayerController.IsSynchronisationActive()
                || TimeManager.IsPaused(TimeManager.PauseLayer.Network)
                || !jetpackPlayerControl.playerControls.enabled)
            {
                StopJet();
                return;
            }

            float heldTime = jetpackPlayerControl.jetpackButton.GetHeldTimeLength();
            if (jetpackPlayerControl.jetpackButton.JustPressed())
                StartJetAndMessage();
            else if (jetpackPlayerControl.jetpackButton.JustReleased())
            {
                if (heldTime > 0.15f)
                    StopJetAndMessage();
                else
                    StartCruiseAndMessage();
            }
        }

        public void OnMessageReceived(bool messageJetting, bool messageCruising)
        {
            if (!jetpackPlayerControl.isServer && jetpackPlayerControl.jetpackButton != null)
                return;
            if (messageJetting) StartJet();
            else StopJet();
            cruising = messageCruising;
        }

        private void StartCruiseAndMessage()
        {
            if (jetpackPlayerControl.isServer)
                jetpackPlayerControl.serverJetpackPlayerControl.StartCruise();
            else
            {
                cruising = true;
                SendClientJetpackMessage(true, true);
            }
        }

        private void StartJetAndMessage()
        {
            if (jetpackPlayerControl.isServer)
                jetpackPlayerControl.serverJetpackPlayerControl.StartJet();
            else
            {
                StartJet();
                SendClientJetpackMessage(true, false);
            }
        }

        public void StartJet()
        {
            cruising = false;
            if (jetting) return;
            jetting = true;
            JetpackPlayerControl.FindSprayEffectPrefab();
            if (JetpackPlayerControl.sprayEffectPrefab == null) return;
            if (jetpackPlayerControl.sprayEffect == null)
            {
                GameObject obj = JetpackPlayerControl.sprayEffectPrefab.InstantiateOnParent(gameObject.transform, true);
                obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                obj.transform.localPosition = Vector3.zero;
                jetpackPlayerControl.sprayEffect = obj.RequireComponent<ParticleSystem>();
                GameUtils.StartAudio(GameLoopingAudioTag.ExtinguisherSpray, this, gameObject.layer);
                jetpackPlayerControl.playerAnimationDecisions.m_animator.SetBool(JetpackPlayerControl.m_iFalling, false);
            }
        }
        private void StopJetAndMessage()
        {
            if (jetpackPlayerControl.isServer)
                jetpackPlayerControl.serverJetpackPlayerControl.StopJet();
            else
            {
                StopJet();
                SendClientJetpackMessage(false, false);
            }
        }

        public void StopJet()
        {
            if (!jetting) return;
            jetting = false;
            cruising = false;
            if (jetpackPlayerControl.sprayEffect != null)
            {
                jetpackPlayerControl.sprayEffect.transform.SetParent(null);
                jetpackPlayerControl.sprayEffect.Stop();
                jetpackPlayerControl.sprayEffect = null;
                GameUtils.StopAudio(GameLoopingAudioTag.ExtinguisherSpray, this);
            }
            jetpackPlayerControl.playerAnimationDecisions.m_animator.SetBool(JetpackPlayerControl.m_iFalling, jetpackPlayerControl.clientPlayerControlsImpl_Default.get_m_isFalling());
        }

        private void SendClientJetpackMessage(bool messageJetting, bool messageCruising)
        {
            JetpackPlayerControl.localClient.SendMessageToServer(JetpackPlayerControl.jetpackMessageType, new JetpackMessage(messageJetting, messageCruising, jetpackPlayerControl.chefEntityID, false), true);
        }
    }

    public class JetpackButton : LogicalButtonBase
    {
        private KeyCode keyCode;
        public JetpackButton(KeyCode keyCode)
        {
            this.keyCode = keyCode;
        }
        public override bool IsDown() => Input.GetKey(keyCode);
        public override void GetLogicTreeData(out AcyclicGraph<ILogicalElement, LogicalLinkInfo> _tree, out AcyclicGraph<ILogicalElement, LogicalLinkInfo>.Node _head)
        {
            _tree = new AcyclicGraph<ILogicalElement, LogicalLinkInfo>(this);
            _head = _tree.GetNode(this);
        }
    }

    public class JetpackMessage : Serialisable
    {
        public JetpackMessage(bool jetting, bool cruising, uint chefEntityID, bool server2client)
        { 
            this.jetting = jetting;
            this.cruising = cruising;
            this.chefEntityID = chefEntityID;
            this.server2client = server2client;
        }

        public void Serialise(BitStreamWriter writer) 
        {
            writer.Write(server2client);
            writer.Write(jetting);
            writer.Write(cruising);
            writer.Write(chefEntityID, 10);
        }

        public bool Deserialise(BitStreamReader reader)
        {
            server2client = reader.ReadBit();
            jetting = reader.ReadBit();
            cruising = reader.ReadBit();
            chefEntityID = reader.ReadUInt32(10);
            return true;
        }

        public bool jetting;
        public bool cruising;
        public uint chefEntityID;
        public bool server2client;
    }
}
