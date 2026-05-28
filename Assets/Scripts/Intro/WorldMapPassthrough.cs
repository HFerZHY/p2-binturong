using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Otowa.Intro
{
    /// <summary>
    /// Scene 4 (WorldMap) placeholder — immediately loads Scene 5.
    /// Setup: empty scene → empty GameObject → attach this script.
    /// Set nextSceneName to match whatever you name the Ryotei scene in Build Settings.
    /// </summary>
    public class WorldMapPassthrough : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "Ryotei";

        private IEnumerator Start()
        {
            yield return null; // one frame for Unity to finish initialising
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
