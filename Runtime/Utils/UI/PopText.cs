using Swift.Core;
using Swift.Core.Pooling;
using UnityEngine;

namespace Swift.Utils
{
    public class PopText : MonoBehaviour, IView
    {
        [SerializeField] private GenericText text = null;
        [SerializeField] private Gradient gradient = new Gradient() {  };
        [SerializeField] private AnimationCurve flyUpCurve = new AnimationCurve();
        [SerializeField] private AnimationCurve scaleCurve = new AnimationCurve();
        [SerializeField] private float duration = 1;
        [SerializeField] private float speed = 1;

        public int InitialPoolCapacity => 100;

        public bool Active { get; set; }

        private IPool pool;

        private float startYPos;

        private float time = 1;

        private Vector3 pos;

        private new Transform transform;

        public void Init(IPool pool)
        {
            this.pool = pool;
        }

        private void Awake()
        {
            transform = base.transform;
        }

        public void ReturnToPool()
        {
            gameObject.SetActive(false);
            pool.Return(this);
        }

        public void SetUp(IViewFactory viewFactory)
        {
            
        }

        public void Show(string txt, Vector3 point, Quaternion rotation)
        {
            gameObject.SetActive(true);
            text.Text = txt;
            transform.position = point;
            transform.rotation = rotation;
            time = 0;
            startYPos = transform.position.y;
            pos = transform.position;
            text.Value.Color = gradient.Evaluate(0);
            transform.localScale = Vector3.one * scaleCurve.Evaluate(0);
        }

        public void TakeFromPool()
        {
            
        }

        public void Process(float delta)
        {
            if(time > duration)
            {
                return;
            }
           
            time += delta;
            float v = time / duration;
            pos.y = Mathf.Lerp(startYPos, startYPos + speed, flyUpCurve.Evaluate(v));
            text.Value.Color = gradient.Evaluate(v);
            transform.localScale = Vector3.one * scaleCurve.Evaluate(v);
            transform.position = pos;
        }

        public GameObject GetRoot()
        {
            return gameObject;
        }

        public void Dispose()
        {
            if(gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }

}
