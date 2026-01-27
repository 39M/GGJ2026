using UnityEngine;
using UnityEngine.Assertions;

namespace Febucci.UI.Examples
{
    /// <summary>
    /// Extra example class for the TextAnimator plugin, used to add sounds to the TextAnimatorPlayer.
    /// </summary>
    [AddComponentMenu("Febucci/TextAnimator/SoundWriter")]
    [RequireComponent(typeof(Core.TypewriterCore))]
    public class TAnimSoundWriter : MonoBehaviour
    {
        private void Awake()
        {
            Assert.IsNotNull(GetComponent<Core.TypewriterCore>(), "TAnimSoundWriter: Component TAnimPlayerBase is not present");

            GetComponent<Core.TypewriterCore>()?.onCharacterVisible.AddListener(OnCharacter);
        }

        void OnCharacter(char character)
        {
            // Play Sound On Character Shown

        }
    }
}