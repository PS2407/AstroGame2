// MissionData.cs
// AstroGame 2.0 - Phase 1
// Location: Assets/_Project/Scripts/Mission/MissionData.cs
//
// A data asset describing one mission. It holds information only, no behaviour.
// Create new missions with: Project panel > right-click > Create > AstroGame > Mission Data.
// The briefing UI reads from this asset, so adding or editing a mission needs no code changes.

using System.Collections.Generic;
using UnityEngine;

namespace AstroGame.Mission
{
    [CreateAssetMenu(fileName = "Mission_New", menuName = "AstroGame/Mission Data", order = 0)]
    public class MissionData : ScriptableObject
    {
        [Tooltip("Short unique code, e.g. M01. Used later for saving progress and analytics.")]
        [SerializeField] private string missionId = "M01";

        [SerializeField] private string title = "Untitled Mission";

        [SerializeField] private string targetPlanet = "Unknown";

        [SerializeField, TextArea(3, 8)] private string description = "";

        [SerializeField] private string[] primaryObjectives = new string[0];

        [SerializeField] private string[] resourcesAndConstraints = new string[0];

        public string MissionId => missionId;
        public string Title => title;
        public string TargetPlanet => targetPlanet;
        public string Description => description;
        public IReadOnlyList<string> PrimaryObjectives => primaryObjectives;
        public IReadOnlyList<string> ResourcesAndConstraints => resourcesAndConstraints;
    }
}
