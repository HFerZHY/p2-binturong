using System;
using UnityEngine;

/// <summary>
/// A data asset that describes a sequence of waypoints an NPC walks through.
/// Create via  Assets ▶ Create ▶ NPC ▶ NPC Path.
/// </summary>
[CreateAssetMenu(menuName = "NPC/NPC Path", fileName = "NewNPCPath")]
public class NPCPath : ScriptableObject
{
    [Serializable]
    public class Waypoint
    {
        [Tooltip("World-space position of this waypoint.")]
        public Vector2 position;

        [Tooltip("How long (seconds) the NPC pauses at this waypoint before continuing.")]
        [Min(0f)] public float stopDuration = 0f;
    }

    [Tooltip("Ordered list of waypoints that form the path.")]
    public Waypoint[] waypoints = Array.Empty<Waypoint>();

    [Tooltip("When true the NPC loops back to waypoint 0 after reaching the last one. " +
             "When false the NPC stops at the last waypoint.")]
    public bool loop = true;

    /// <summary>Returns false when the path has fewer than two waypoints.</summary>
    public bool IsValid => waypoints != null && waypoints.Length >= 2;
}