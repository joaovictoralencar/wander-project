using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LukeyB.DeepStats.Demo
{
    public class SceneSwapping : MonoBehaviour
    {
        public List<string> SceneNames;

        private int _currentScene = 0;
        private float _nextSwap = 3;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.time < _nextSwap)
            {
                return;
            }

            _nextSwap = Time.time + 3;

            SceneManager.LoadScene(SceneNames[_currentScene]);
            _currentScene++;

            if (_currentScene == SceneNames.Count)
            {
                _currentScene = 0;
            }
        }
    }
}
