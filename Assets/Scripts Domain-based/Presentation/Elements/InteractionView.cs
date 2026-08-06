using System.Collections;
using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;

namespace Presentation.Elements
{
    public class InteractionView : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private TMP_Text textInteractionHint;

        private void Start()
        {
            if (PlayerInteractor.Instance == null)
            {
                Debug.LogError("Player Interactor is not initialized");
                return;
            }

            PlayerInteractor.Instance.CurrentInteraction
                .Subscribe(x =>
                {
                    if (x == null)
                    {
                        textInteractionHint.text = string.Empty;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(x.Hint))
                            textInteractionHint.text = $"E - Взаимодействовать";
                        else
                            textInteractionHint.text = $"E - {x.Hint}";
                    }
                })
                .AddTo(this);
        }
    }
}
