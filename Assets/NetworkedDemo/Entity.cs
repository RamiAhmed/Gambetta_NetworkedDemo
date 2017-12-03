namespace NetworkedDemo
{
    using System.Collections.Generic;
    using UnityEngine;

    public class Entity
    {
        public Vector2 position;
        public float speed = 10f; // units/s
        public int entity_id;
        public List<TimedPosition> position_buffer = new List<TimedPosition>();

        public void ApplyInput(Input input)
        {
            this.position += input.move_input * this.speed;
        }
    }
}