using System;
using UnityEngine;
using UnityEngine.UI;

namespace Swift.Utils.UI
{
    public class ClickReminder : MonoBehaviour
    {
        [SerializeField] private Button button = null;
        [SerializeField] private int durationHours = 24;
        [SerializeField] private GameObject reminderIcon = null;
        
        private void Start()
        {
            button.onClick.AddListener(OnClick);
            if (DateTime.TryParse(PlayerPrefs.GetString(name + "_reminder", ""), out DateTime remindTime))
            {
                bool expired = DateTime.UtcNow - remindTime > TimeSpan.FromHours(durationHours);
                reminderIcon.SetActive(expired);
            }
            else
            {
                reminderIcon.SetActive(true);
            }
        }

        private void OnClick()
        {
            reminderIcon.SetActive(false);
            PlayerPrefs.SetString(name + "_reminder", DateTime.UtcNow.ToString());
            PlayerPrefs.Save();
        }
    }
}