using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Utils
{
    public static class SerializedFieldChecker
    {
        /// <summary>
        /// Проверяет, заполнены ли все [SerializeField] поля у объекта, и логирует предупреждения, если поля не заполнены.
        /// Работает только в режиме DEBUG (Editor или Debug Build).
        /// </summary>
        /// <param name="behaviour">Объект, который нужно проверить.</param>
        /// <param name="detailedLog">Если true, выводит подробный лог о незаполненных полях.</param>
        public static void ValidateSerializedFields(this MonoBehaviour behaviour, bool detailedLog = true)
        {
#if UNITY_EDITOR
            if (behaviour == null)
            {
                Debug.LogError("Объект для проверки равен null.");
                return;
            }

            var type = behaviour.GetType();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var allFieldsAssigned = true;

            foreach (var field in fields)
            {
                if (Attribute.IsDefined(field, typeof(SerializeField)))
                {
                    var value = field.GetValue(behaviour);

                    if (IsUnassigned(value))
                    {
                        if (detailedLog)
                            Debug.LogWarning(
                                $"[SerializeField] Поле '{field.Name}' в объекте '{behaviour.name}' не присвоено.",
                                behaviour);

                        allFieldsAssigned = false;
                    }
                }
            }

            if (!allFieldsAssigned)
            {
                Debug.LogError($"Компонент '{behaviour.GetType().Name}' содержит незаполненные [SerializeField] поля!",
                    behaviour);
            }
#endif
        }

        /// <summary>
        /// Проверяет, является ли значение неинициализированным.
        /// </summary>
        private static bool IsUnassigned(object value)
        {
            return value switch
            {
                UnityEngine.Object unityObject => unityObject == null,
                _ => value == null
            };
        }
    }
}
