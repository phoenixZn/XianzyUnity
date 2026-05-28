using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IngameDebugConsole
{
    public class CustomDebugCommands : MonoBehaviour
    {
        [SerializeField]
        private GameObject Templet;
        
        
        void Start()
        {
            if (Templet == null && transform.childCount > 0)
            {
                Templet = transform.GetChild(0).gameObject;
            }

            if (Templet != null)
            {
                Templet.SetActive(false);
            }
        }

        public GameObject GetTemplate()
        {
            return Templet;
        }

        public void RemoveInjectedButtonByName(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
            {
                return;
            }

            Transform child = transform.Find(buttonName);
            if (child == null)
            {
                return;
            }

            if (Templet != null && child.gameObject == Templet)
            {
                return;
            }

            Destroy(child.gameObject);
        }

        public GameObject CreateInjectedButton()
        {
            if (Templet == null)
            {
                return null;
            }

            GameObject go = Instantiate(Templet, transform);
            go.SetActive(true);
            return go;
        }
    }
}

