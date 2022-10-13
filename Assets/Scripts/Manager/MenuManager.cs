using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Manager
{
    public class MenuManager
    {
        private readonly Dictionary<string ,GameObject > _menus;

        public MenuManager()
        {
            _menus = GameManager.instance.GetMenus();
            foreach (var t in _menus)
            {
                t.Value.SetActive(t.Key == "LoadingMenu");
            }
        }

        public void OpenMenu(string menuName)
        {
            _menus[menuName].SetActive(true);
            foreach (var t in _menus.Where(t => t.Key != menuName))
            {
                t.Value.SetActive(false);
            }
        }

        public void CloseMenu(string menuName)
        {
            _menus[menuName].SetActive(false);
        }
    }
}