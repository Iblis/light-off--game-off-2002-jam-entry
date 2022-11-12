using JetBrains.Annotations;
using RailgunNet.Connection.Server;
using RailgunNet.Factory;
using RailgunNet.Logic.Wrappers;
using RailgunNet.System.Buffer;
using RailgunNet.System.Types;
using RailgunNet.Util.Debug;
using RailgunNet.Util.Pooling;

namespace RailgunNet.Logic
{
    /// <summary>
    ///     Handy shortcut class for auto-casting the state.
    /// </summary>
    public class RailEntityServer<TState> : RailEntityServer
        where TState : RailState
    {
        #region Public API
        /// <summary>
        ///     Returns the current local state.
        /// </summary>
        [PublicAPI]
        public TState State { get; private set; }
        #endregion

        protected override RailState StateBase
        {
            get => State;
            set => State = (TState) value;
        }
    }

    /// <summary>
    ///     Handy shortcut class for auto-casting the state and command.
    /// </summary>
    public class RailEntityServer<TState, TCommand> : RailEntityServer<TState>
        where TState : RailState, new()
        where TCommand : RailCommand
    {
        #region Public API
        /// <summary>
        ///     Applies a command to this instance.
        ///     Called on controller and server.
        /// </summary>
        /// <param name="toApply"></param>
        [PublicAPI]
        protected virtual void ApplyCommand(TCommand toApply)
        {
        }
        #endregion

        protected sealed override void ApplyControlGeneric(RailCommand toApply)
        {
            ApplyCommand((TCommand) toApply);
        }
    }

    public abstract class RailEntityServer : RailEntityBase
    {
        private readonly RailDejitterBuffer<RailCommand> incomingCommands;
        private readonly RailQueueBuffer<RailStateRecord> outgoingStates;

        // The remote (client) tick of the last command we processed
        private Tick commandAck;

        // The controller at the time of entity removal
        private RailController priorController;

        public RailEntityServer()
        {
            // We use no divisor for storing commands because commands are sent in
            // batches that we can use to fill in the holes between send ticks
            incomingCommands =
                new RailDejitterBuffer<RailCommand>(RailConfig.DEJITTER_BUFFER_LENGTH);
            outgoingStates =
                new RailQueueBuffer<RailStateRecord>(RailConfig.DEJITTER_BUFFER_LENGTH);
            Reset();
        }

        public override RailRoom RoomBase
        {
            get => Room;
            set => Room = (RailServerRoom) value;
        }

        [PublicAPI] protected RailServerRoom Room { get; private set; }

        public void MarkForRemoval()
        {
            // We'll remove on the next tick since we're probably 
            // already mid-way through evaluating this tick
            RemovedTick = RoomBase.Tick + 1;

            // Notify our inheritors that we are being removed next tick
            OnSunset();
        }

        public void ServerUpdate()
        {
            UpdateAuthoritative();

            RailCommand latest = GetLatestCommand();
            if (latest != null)
            {
                ApplyControlGeneric(latest);
                latest.IsNewCommand = false;

                // Use the remote tick rather than the last applied tick
                // because we might be skipping some commands to keep up
                UpdateCommandAck(Controller.EstimatedRemoteTick);
            }
            else if (Controller != null)
            {
                // We have no command to work from but might still want to
                // do an update in the command sequence (if we have a controller)
                CommandMissing();
            }
        }

        public static T Create<T>(RailResource resource)
            where T : RailEntityServer
        {
            int factoryType = resource.GetEntityFactoryType<T>();
            return (T) Create(resource, factoryType);
        }

        public void StoreRecord(IRailStateConstruction stateCreator)
        {
            RailStateRecord record = RailStateRecordFactory.Create(
                stateCreator,
                RoomBase.Tick,
                StateBase,
                outgoingStates.Latest);
            if (record != null) outgoingStates.Store(record);
        }

        public RailStateDelta ProduceDelta(
            IRailStateConstruction stateCreator,
            Tick basisTick,
            RailController destination,
            bool forceAllMutable)
        {
            // Flags for special data modes
            bool includeControllerData =
                destination == Controller || destination == priorController;
            bool includeImmutableData = basisTick.IsValid == false;

            return RailStateDeltaFactory.Create(
                stateCreator,
                Id,
                StateBase,
                outgoingStates.LatestFrom(basisTick),
                includeControllerData,
                includeImmutableData,
                commandAck,
                RemovedTick,
                forceAllMutable);
        }

        #region Lifecycle and Loop
        public sealed override void Removed()
        {
            RailDebug.Assert(HasStarted);

            // Automatically revoke control but keep a history for 
            // sending the final controller data to the client.
            if (Controller != null)
            {
                priorController = Controller;
                Controller.RevokeControlInternal(this);
                NotifyControllerChanged();
            }

            base.Removed();
        }
        #endregion

        protected sealed override void Reset()
        {
            base.Reset();
            outgoingStates.Clear();
            incomingCommands.Clear();

            ResetStates();
            OnReset();
        }

        private void ResetStates()
        {
            if (StateBase != null) RailPool.Free(StateBase);

            StateBase = null;
        }

        protected sealed override void ClearCommands()
        {
            incomingCommands.Clear();
            commandAck = Tick.INVALID;
        }

        public void ReceiveCommand(RailCommand command)
        {
            if (incomingCommands.Store(command))
            {
                command.IsNewCommand = true;
            }
            else
            {
                RailPool.Free(command);
            }
        }

        private RailCommand GetLatestCommand()
        {
            if (Controller != null)
            {
                return incomingCommands.GetLatestAt(Controller.EstimatedRemoteTick);
            }

            return null;
        }

        private void UpdateCommandAck(Tick latestCommandTick)
        {
            bool shouldAck = commandAck.IsValid == false || latestCommandTick > commandAck;
            if (shouldAck) commandAck = latestCommandTick;
        }

        #region Override Functions
        /// <summary>
        ///     Called if the entity had no pending command this tick.
        ///     Called on server.
        /// </summary>
        [PublicAPI]
        protected virtual void CommandMissing()
        {
        }

        /// <summary>
        ///     Called first in an update, before processing a command. Clients will obey
        ///     to this state for all non-controlled entities.
        ///     Called on server.
        /// </summary>
        [PublicAPI]
        protected virtual void UpdateAuthoritative()
        {
        }

        /// <summary>
        ///     When the entity will be removed on the next tick.
        ///     Called on server.
        /// </summary>
        [PublicAPI]
        protected virtual void OnSunset()
        {
        } // Called on server
        #endregion
    }
}
