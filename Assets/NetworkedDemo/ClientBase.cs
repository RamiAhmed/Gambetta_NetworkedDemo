namespace NetworkedDemo
{
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class ClientBase
    {
        // Local representation of the entities.
        public List<Entity> entities = new List<Entity>();

        // Input state.
        public bool key_left = false;

        public bool key_right = false;
        public bool key_up = false;
        public bool key_down = false;

        // Unique ID of our entity. Assigned by Server on connection.
        public int? entity_id = null;

        // Data needed for reconciliation.
        public bool client_side_prediction = false;

        public bool server_reconciliation = false;
        public int input_sequence_number = 0;
        public List<Input> pending_inputs = new List<Input>();

        // Entity interpolation toggle.
        public bool entity_interpolation = true;

        // Update rate
        // Default update rate.
        public ulong update_rate = 50;

        public Date last_ts;

        // :: Additions
        private ulong _nextUpdate;

        public abstract int averageRoundTripTimeMS
        {
            get;
        }

        public void Create(int entity_id)
        {
            this.entity_id = entity_id;

            //if (this.entities.Count <= entity_id || this.entities[entity_id] == null)
            //{
            //    var newEntity = new Entity();
            //    if (this.entities.Count <= entity_id)
            //    {
            //        this.entities.Add(newEntity);
            //    }
            //    else
            //    {
            //        this.entities[entity_id] = newEntity;
            //    }
            //}

            //var entity = this.entities[entity_id];
            //entity.entity_id = entity_id;

            // Set the initial state of the Entity (e.g. spawn point)
            //entity.x = Constants.SpawnPoints[entity_id];
        }

        public void SetUpdateRate(ulong hz)
        {
            this.update_rate = hz;
        }

        // :: Addition
        public void Tick()
        {
            var now = new Date();
            if (now < _nextUpdate)
            {
                return;
            }

            _nextUpdate = now + this.update_rate;
            Update();
        }

        // Update Client state.
        private void Update()
        {
            // Listen to the server.
            this.ProcessServerMessages();

            if (this.entity_id == null)
            {
                return; // Not connected yet.
            }

            // Process inputs.
            this.ProcessInputs();

            // Interpolate other entities.
            if (this.entity_interpolation)
            {
                this.InterpolateEntities();
            }

            // Render the World.
            RenderWorld();

            // Show some info.
            //var info = "Non-acknowledged inputs: " + this.pending_inputs.length;
            //this.status.textContent = info;
        }

        protected abstract void ProcessServerMessages();

        protected abstract void Send(Input input);

        internal void OnData(EntityState[] world_state)
        {
            // World state is a list of entity states.
            for (var i = 0; i < world_state.Length; i++)
            {
                var state = world_state[i];
                if (state.entity_id == 0 &&
                    state.last_processed_input == 0 &&
                    state.position == Vector2.zero)
                {
                    continue;
                }

                // If this is the first time we see this entity, create a local representation.
                if (this.entities.Count <= state.entity_id || this.entities[state.entity_id] == null)
                {
                    // :: Addition (changed name entity => newEntity to avoid compile error)
                    var newEntity = new Entity();
                    newEntity.entity_id = state.entity_id;

                    if (this.entities.Count <= state.entity_id)
                    {
                        this.entities.Add(newEntity);
                    }
                    else
                    {
                        this.entities[state.entity_id] = newEntity;
                    }
                }

                var entity = this.entities[state.entity_id];

                if (state.entity_id == this.entity_id)
                {
                    // Received the authoritative position of this client's entity.
                    entity.position = state.position;

                    if (this.server_reconciliation)
                    {
                        // Server Reconciliation. Re-apply all the inputs not yet processed by
                        // the server.
                        var j = 0;
                        while (j < this.pending_inputs.Count)
                        {
                            var input = this.pending_inputs[j];
                            if (input.input_sequence_number <= state.last_processed_input)
                            {
                                // Already processed. Its effect is already taken into account into the world update
                                // we just got, so we can drop it.
                                this.pending_inputs.RemoveAt(j);
                            }
                            else
                            {
                                // Not processed by the server yet. Re-apply it.
                                entity.ApplyInput(input);
                                j++;
                            }
                        }
                    }
                    else
                    {
                        // Reconciliation is disabled, so drop all the saved inputs.
                        this.pending_inputs.Clear();
                    }
                }
                else
                {
                    // Received the position of an entity other than this client's.

                    if (!this.entity_interpolation)
                    {
                        // Entity interpolation is disabled - just accept the server's position.
                        entity.position = state.position;
                    }
                    else
                    {
                        // Add it to the position buffer.
                        var timestamp = new Date();
                        entity.position_buffer.Add(new TimedPosition()
                        {
                            timestamp = timestamp,
                            position = state.position
                        });
                    }
                }
            }
        }

        private void ProcessInputs()
        {
            // Compute delta time since last update.
            var now_ts = new Date();
            var last_ts = this.last_ts ?? now_ts;
            var dt_sec = (float)((now_ts - last_ts) / 1000d);
            this.last_ts = now_ts;

            // Package player's input.
            var moveInput = Vector2.zero;
            if (this.key_right)
            {
                moveInput.x = 1f;
            }
            else if (this.key_left)
            {
                moveInput.x = -1f;
            }

            if (this.key_up)
            {
                moveInput.y = 1f;
            }
            else if (this.key_down)
            {
                moveInput.y = -1f;
            }

            if (moveInput == Vector2.zero)
            {
                // Nothing interesting happened.
                return;
            }

            var input = new Input()
            {
                move_input = moveInput.normalized * dt_sec
            };

            // Send the input to the server.
            input.input_sequence_number = this.input_sequence_number++;
            input.entity_id = this.entity_id.Value;
            Send(input);

            // Do client-side prediction.
            if (this.client_side_prediction)
            {
                this.entities[this.entity_id.Value].ApplyInput(input);
            }

            // Save this input for later reconciliation.
            this.pending_inputs.Add(input);
        }

        private void InterpolateEntities()
        {
            // Compute render timestamp.
            var now = new Date();
            var render_timestamp = now - (1000d / Constants.ServerUpdateRate);

            for (int i = 0; i < this.entities.Count; i++)
            {
                var entity = this.entities[i];

                // No point in interpolating this client's entity.
                if (entity.entity_id == this.entity_id)
                {
                    continue;
                }

                // Find the two authoritative positions surrounding the rendering timestamp.
                var buffer = entity.position_buffer;

                // Drop older positions.
                while (buffer.Count >= 2 && buffer[1].timestamp <= render_timestamp)
                {
                    buffer.RemoveAt(0);
                }

                // Interpolate between the two surrounding authoritative positions.
                if (buffer.Count >= 2 && buffer[0].timestamp <= render_timestamp && render_timestamp <= buffer[1].timestamp) // :: Addition (instead of nested array, use struct to access named fields)
                {
                    var x0 = buffer[0].position;
                    var x1 = buffer[1].position;
                    var t0 = buffer[0].timestamp;
                    var t1 = buffer[1].timestamp;

                    entity.position = Vector2.Lerp(x0, x1, (float)(render_timestamp - t0) / (t1 - t0));
                    //entity.position = (x0 + (x1 - x0) * (render_timestamp - t0) / (t1 - t0));
                }
            }
        }

        private void RenderWorld()
        {
            NetworkedWorldCanvas.instance.RenderWorld(this.entities);
        }

        internal virtual void TearDown()
        {
            this.entity_id = null;
        }
    }
}