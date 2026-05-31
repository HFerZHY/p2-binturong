using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Otowa.IndoorDialogue
{
    /// <summary>
    /// Shared typewriter for indoor visual-novel scenes.
    /// Map dialogue has separate paging and interaction rules in DialogueUIController.
    /// </summary>
    public class IndoorDialogueTextPlayer : MonoBehaviour
    {
        private TMP_Text _promptText;
        private TMP_Text _activeText;
        private Coroutine _typewriterCoroutine;
        private Action _onComplete;
        private float _charactersPerSecond;

        public bool IsTyping => _typewriterCoroutine != null;

        public void Initialize(TMP_Text promptText, float charactersPerSecond)
        {
            _promptText = promptText;
            _charactersPerSecond = charactersPerSecond;
        }

        public void Play(TMP_Text target, string text, Action onComplete = null)
        {
            Cancel();

            _activeText = target;
            _onComplete = onComplete;
            _activeText.text = text ?? string.Empty;
            _activeText.ForceMeshUpdate();
            _activeText.maxVisibleCharacters = 0;
            SetPromptVisible(false);

            int characterCount = _activeText.textInfo.characterCount;
            if (characterCount == 0)
            {
                Finish();
                return;
            }

            _typewriterCoroutine = StartCoroutine(Typewriter(characterCount));
        }

        public void Skip()
        {
            if (_typewriterCoroutine == null) return;

            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
            if (_activeText != null)
                _activeText.maxVisibleCharacters = int.MaxValue;
            Finish();
        }

        public void Cancel()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _activeText = null;
            _onComplete = null;
        }

        public void SetPromptVisible(bool visible)
        {
            if (_promptText != null)
                _promptText.gameObject.SetActive(visible);
        }

        private IEnumerator Typewriter(int characterCount)
        {
            float delay = 1f / Mathf.Max(1f, _charactersPerSecond);
            for (int i = 1; i <= characterCount; i++)
            {
                _activeText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(delay);
            }

            _typewriterCoroutine = null;
            Finish();
        }

        private void Finish()
        {
            SetPromptVisible(true);
            var onComplete = _onComplete;
            _onComplete = null;
            onComplete?.Invoke();
        }

        private void OnDisable()
        {
            Cancel();
        }
    }
}
