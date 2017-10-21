﻿namespace VehicleGadgetsPlus.VehicleGadgets
{
    using System;
    using System.Windows.Forms;
    using System.Linq;

    using Rage;
    
    using VehicleGadgetsPlus.VehicleGadgets.XML;

    internal sealed class Outriggers : VehicleGadget
    {
        enum OutriggersState
        {
            Undeployed,
            Undeploying,
            Deploying,
            Deployed,
        }

        enum UpDownState
        {
            None,
            Up,
            Down,
        }

        private readonly OutriggersEntry outriggersDataEntry;
        private readonly Outrigger[] outriggers;

        private readonly string loopSoundId;
        private bool wasAnyOutriggerMoving;

        public Outriggers(Vehicle vehicle, VehicleGadgetEntry dataEntry) : base(vehicle, dataEntry)
        {
            outriggersDataEntry = (OutriggersEntry)dataEntry;

            outriggers = new Outrigger[outriggersDataEntry.Outriggers.Length];

            for (int i = 0; i < outriggersDataEntry.Outriggers.Length; i++)
            {
                outriggers[i] = new Outrigger(vehicle, outriggersDataEntry.Outriggers[i]);
            }

            if (outriggersDataEntry.HasSoundsSet)
            {
                loopSoundId = $"outriggers_loop_{Guid.NewGuid()}";
            }
        }

        public override void Update(bool isPlayerIn)
        {
            bool isAnyOutriggerMoving = false; 

            float delta = Game.FrameTime;
            for (int i = 0; i < outriggers.Length; i++)
            {
                Outrigger r = outriggers[i];
                r.Update(delta);
                isAnyOutriggerMoving = r.State == OutriggersState.Deploying || r.State == OutriggersState.Undeploying || r.VerticalState != UpDownState.None;
            }

            if (outriggersDataEntry.HasSoundsSet)
            {
                if (isAnyOutriggerMoving)
                {
                    if (!wasAnyOutriggerMoving && !IsPlayingLoopSound())
                    {
                        PlayLoopSound();
                    }
                }
                else
                {
                    if (wasAnyOutriggerMoving && IsPlayingLoopSound())
                    {
                        StopLoopSound();
                    }
                }
            }

            wasAnyOutriggerMoving = isAnyOutriggerMoving;

            if (isPlayerIn && Game.IsKeyDown(Keys.O))
            {
                if (outriggers.All(o => o.State == OutriggersState.Undeployed))
                {
                    foreach (Outrigger o in outriggers)
                    {
                        o.State = OutriggersState.Deploying;
                    }
                }
                else if (outriggers.All(o => o.State == OutriggersState.Deployed))
                {
                    foreach (Outrigger o in outriggers)
                    {
                        o.State = OutriggersState.Undeploying;
                        o.VerticalState = UpDownState.Up;
                    }
                }
            }
        }

        private bool IsPlayingLoopSound()
        {
            return Plugin.SoundPlayer.IsPlaying(loopSoundId);
        }

        private void PlayLoopSound()
        {
            if (!outriggersDataEntry.HasSoundsSet)
                return;

            if (outriggersDataEntry.SoundsSet.IsDefaultLoop)
            {
                Plugin.SoundPlayer.Play(loopSoundId, true, outriggersDataEntry.SoundsSet.NormalizedVolume, () => Properties.Resources.default_outriggers_loop);
            }
            else
            {
                // TODO: implement custom sound loading
            }
        }

        private void StopLoopSound()
        {
            if (!outriggersDataEntry.HasSoundsSet)
                return;

            Plugin.SoundPlayer.Stop(loopSoundId);
        }


        private class Outrigger
        {
            private readonly Vehicle vehicle;
            private readonly OutriggersEntry.Outrigger data;
            private readonly Vector3 extensionDirection,
                                     supportDirection;
            private readonly VehicleBone extensionBone,
                                         supportBone;

            float currentExtendDistance;
            float currentSupportDistance;

            // TODO: read state from distances between current and original positions, otherwise when repaired and outriggers deployed, will mess things up
            public OutriggersState State { get; set; }
            public UpDownState VerticalState { get; set; }

            public Outrigger(Vehicle vehicle, OutriggersEntry.Outrigger data)
            {
                this.vehicle = vehicle;
                this.data = data;

                extensionDirection = data.ExtensionDirection;
                supportDirection = data.SupportDirection;

                if (!VehicleBone.TryGetForVehicle(vehicle, data.ExtensionBoneName, out extensionBone))
                {
                    throw new InvalidOperationException($"The model \"{vehicle.Model.Name}\" doesn't have the bone \"{data.ExtensionBoneName}\" for the Outrigger Extension");
                }

                if (!VehicleBone.TryGetForVehicle(vehicle, data.SupportBoneName, out supportBone))
                {
                    throw new InvalidOperationException($"The model \"{vehicle.Model.Name}\" doesn't have the bone \"{data.SupportBoneName}\" for the Outrigger Support");
                }
            }

            public void Update(float delta)
            {
                switch (State)
                {
                    case OutriggersState.Undeploying:
                        {
                            if (VerticalState == UpDownState.None) // wait for the legs to go up
                            {
                                float moveDist = data.ExtensionMoveSpeed * delta;
                                Vector3 translation = -extensionDirection * moveDist;

                                currentExtendDistance -= Math.Abs(moveDist);

                                extensionBone.Translate(translation);

                                if (currentExtendDistance <= 0.0f)
                                {
                                    State = OutriggersState.Undeployed;
                                }
                            }
                        }
                        break;
                    case OutriggersState.Deploying:
                        {
                            float moveDist = data.ExtensionMoveSpeed * delta;
                            Vector3 translation = extensionDirection * moveDist;

                            currentExtendDistance += Math.Abs(moveDist);

                            extensionBone.Translate(translation);

                            if (currentExtendDistance >= data.ExtensionDistance)
                            {
                                State = OutriggersState.Deployed;
                                VerticalState = UpDownState.Down;
                            }
                        }
                        break;
                }


                switch (VerticalState)
                {
                    case UpDownState.Up:
                        {
                            float moveDist = data.SupportMoveSpeed * delta;
                            Vector3 translation = -supportDirection * moveDist;

                            currentSupportDistance -= Math.Abs(moveDist);

                            supportBone.Translate(translation);

                            if (currentSupportDistance <= 0.0f)
                            {
                                VerticalState = UpDownState.None;
                            }
                        }
                        break;
                    case UpDownState.Down:
                        {
                            float moveDist = data.SupportMoveSpeed * delta;
                            Vector3 translation = supportDirection * moveDist;

                            currentSupportDistance += Math.Abs(moveDist);

                            supportBone.Translate(translation);

                            if (currentSupportDistance >= data.SupportDistance)
                            {
                                VerticalState = UpDownState.None;
                            }
                        }
                        break;
                }
            }
        }
    }
}
