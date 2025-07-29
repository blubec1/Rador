using UnityEngine;

namespace QuickStart
{
    //pentru cazul in care SceneScript nu se initializeaza la timp ca PlayerScript sa-l acceseze
    public class SceneReference : MonoBehaviour
    {
        public SceneScript sceneScript;
    }
}