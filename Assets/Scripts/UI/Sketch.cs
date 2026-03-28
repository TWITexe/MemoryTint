using UnityEngine;

namespace UI
{
    [System.Serializable]
    public class Sketch
    {
        [SerializeField]
        private Sprite image;

        [SerializeField]
        private string caption;

        public Sprite Image => image;
        public string Caption => caption;

        public Sketch()
        {
        }

        public Sketch(Sprite imageValue, string captionValue)
        {
            image = imageValue;
            caption = captionValue;
        }
    }
}
