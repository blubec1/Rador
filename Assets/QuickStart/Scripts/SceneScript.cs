using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace QuickStart
{

    //scriptul care se ocupa cu orice tine de butoane si UI, folosit mai ales pentru a sti ce player a votat ce si pentru a actualiza UI-ul si a ii da enable/disable
    public class SceneScript : NetworkBehaviour
    {
        public TextMeshProUGUI canvasStatusText;
        public PlayerGeneral PG;
        public SceneReference sceneReference;
        public Canvas inGameUI;
        public Canvas inBetweenRoundsUI;
        public GameObject scoreBoard;
        [SyncVar(hook = nameof(OnStatusTextChanged))]
        public string statusText;
        public TextMeshProUGUI canvasAmmoText, canvasHPText, canvasArmorText, winnerText;
        public GameState gs;

        public void UIAmmo(int _value)
        {
            canvasAmmoText.text = "Ammo: " + _value;
        }

        public void UIHP(int _value)
        {
            canvasHPText.text = "HP: " + _value;
        }

        public void UIArmor(int _value)
        {
            canvasArmorText.text = "Armor: " + _value;
        }

        void OnStatusTextChanged(string _Old, string _New)
        {
            canvasStatusText.text = statusText;
        }
        public void StartRoundUI()
        {
            inGameUI.gameObject.SetActive(true);
            inBetweenRoundsUI.gameObject.SetActive(false);
        }
        public void EndRoundUI(string winnerInfo)
        {
            inGameUI.gameObject.SetActive(false);
            inBetweenRoundsUI.gameObject.SetActive(true);
            winnerText.text = winnerInfo + " won the round";
        }
        public void ShowScoreBoard()
        {
            scoreBoard.gameObject.SetActive(true);
        }
        public void HideScoreBoard()
        {
            scoreBoard.gameObject.SetActive(false);
        }
        public void VoteDM()
        {
            NetworkClient.connection.identity.gameObject.GetComponent<PlayerGeneral>().VoteDM();
        }
        public void VoteTDM()
        {
            NetworkClient.connection.identity.gameObject.GetComponent<PlayerGeneral>().VoteTDM();
        }

    }
}