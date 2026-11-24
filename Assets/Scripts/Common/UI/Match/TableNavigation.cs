using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Shared table navigation for settings, help, exit and results.</summary>
    public abstract partial class TableNavigation : CanvasNavigation
    {
        protected override string InitialScreen => "MatchScreen";
        protected abstract void LeaveTable();
        protected virtual void Replay() => GoTo(gameObject.scene.name);
        protected virtual void ExitResults() => LeaveTable();

        protected override void BindControls()
        {
            BindButton(InitialScreen,"ElementHelpButton", () => Open("ElementHelpDialog"));
            BindButton(InitialScreen,"PauseButton", () => Open("PauseDialog"));
            BindButton("PauseDialog","ResumeButton",Close);
            BindButton("PauseDialog","SettingsButton", () => Open("SettingsDialog"));
            BindButton("PauseDialog","ElementHelpButton", () => Open("ElementHelpDialog"));
            BindButton("PauseDialog","LeaveMatchButton", () => Open("LeaveMatchDialog"));
            BindButton("PauseDialog","PreviewButton", () => Open("PreviewDialog"));
            BindButton("SettingsDialog","CloseSettingsButton",Close);
            BindButton("LeaveMatchDialog","StayButton",Close);
            BindButton("LeaveMatchDialog","LeaveButton",LeaveTable);
            BindButton("ResultsDialog","RematchButton",Replay);
            BindButton("ResultsDialog","ReturnButton",ExitResults);
        }
    }
}
