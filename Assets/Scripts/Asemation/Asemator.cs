using UnityEngine;

namespace Asemation
{
    public class Asemator : MonoBehaviour
    {
        public Object aseFile;
        public int pixelPerUnit;
        public string asePath;

        AseInfo _aseInfo;
        AseInfo.Tag _currentTag;
        int _currentFrame;
        bool _loop;
        float _frameTime;

        SpriteRenderer _renderer;
        MaterialPropertyBlock _mpb;

        void UpdateTexture()
        {
            Color[] pixels = _aseInfo.frames[_currentFrame].pixels;

            Texture2D texture = new Texture2D((int)Mathf.Sqrt(pixels.Length), (int)Mathf.Sqrt(pixels.Length));

            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % (int)Mathf.Sqrt(pixels.Length);
                int y = (int)Mathf.Sqrt(pixels.Length) - (i / (int)Mathf.Sqrt(pixels.Length)) - 1;
                texture.SetPixel(x, y, pixels[i]);
            }

            texture.filterMode = FilterMode.Point;
            texture.Apply();

            Rect rect = new Rect(0, 0, _aseInfo.width, _aseInfo.height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), pixelPerUnit);
            _renderer.sprite = sprite;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
            _aseInfo = new AseInfo(asePath);
        }

        void Start()
        {
            SetAnimation("idle", true);
        }

        void Update()
        {
            _frameTime += Time.deltaTime;
            if (_frameTime < _aseInfo.frames[_currentFrame].duration / 1000) return;
            _currentFrame++;
            if (_currentFrame > _currentTag.to)
            {
                if (_loop) _currentFrame = _currentTag.from;
                else _currentFrame = _currentTag.to;
            }
            _frameTime = 0;
            UpdateTexture();
        }

        public void SetAnimation(string tag, bool loop)
        {
            _currentTag = _aseInfo.tags.Find(i => i.name == tag);
            _loop = loop;
            _frameTime = 0;
            _currentFrame = _currentTag.from;
            UpdateTexture();
        }

        public bool IsPlaying(string tag) => _currentTag.name == tag;
    }
}