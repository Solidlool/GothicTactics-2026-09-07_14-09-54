using GothicTactics.Units;
using UnityEngine;

namespace GothicTactics.Combat
{
    [RequireComponent(typeof(TurnManager))]
    public sealed class PrototypeTurnHUD : MonoBehaviour
    {
        private TurnManager turnManager;
        private GUIStyle panelStyle;
        private GUIStyle headingStyle;

        private void Awake()
        {
            turnManager = GetComponent<TurnManager>();
        }

        private void OnGUI()
        {
            panelStyle ??= new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 12, 12) };
            headingStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };

            GUILayout.BeginArea(new Rect(16f, 16f, 260f, 150f), panelStyle);
            var activeUnit = turnManager.ActiveUnit;
            GUILayout.Label($"Round {turnManager.Round}", headingStyle);
            GUILayout.Label(activeUnit == null ? "Preparing encounter..." : $"Turn: {activeUnit.name}");
            if (activeUnit != null)
            {
                GUILayout.Label($"AP: {activeUnit.CurrentActionPoints} / {activeUnit.MaximumActionPoints}");
                GUI.enabled = activeUnit.Team == UnitTeam.Player && !activeUnit.IsMoving;
                if (GUILayout.Button("End Turn", GUILayout.Height(32f))) turnManager.RequestEndTurn();
                GUI.enabled = true;
            }
            GUILayout.EndArea();
        }
    }
}
