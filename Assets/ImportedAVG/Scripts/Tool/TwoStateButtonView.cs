using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AVG {
	public class TwoStateButtonView : MonoBehaviour {
		[SerializeField] 
		private Button _btnAutoPlay;
		[SerializeField] 
		private GameObject _stateOff;
		[SerializeField]
		private GameObject _stateOn;

		private bool m_isOn;
		
		public bool isOn => m_isOn;
		
		public void SetBtnAction(UnityAction btnAction) {
			_btnAutoPlay.onClick.AddListener(btnAction);
		}

		public void Render(bool isOn) {
			m_isOn = isOn;
			_stateOff.SetActive(!isOn);
			_stateOn.SetActive(isOn);
		}
	}
}