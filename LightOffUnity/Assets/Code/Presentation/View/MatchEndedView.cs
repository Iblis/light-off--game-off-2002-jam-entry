using UnityEngine.UIElements;

namespace LightOff.Presentation.View
{
    public class MatchEndedView : ViewBase
    {
        public Label WinnerName {  get; private set; }
        public Button LeaveButton { get; private set; }

        public void Awake()
        {
            _rootElement = gameObject.GetComponent<UIDocument>().rootVisualElement;
            WinnerName = _rootElement.Q<Label>("winnerName");
            LeaveButton = _rootElement.Q<Button>("leaveButton");
        }
    }
}
