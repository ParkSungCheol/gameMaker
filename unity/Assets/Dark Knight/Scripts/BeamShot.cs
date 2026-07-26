using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TealFalconEnemySeries{

    public class BeamShot : MonoBehaviour
    {

        public Rigidbody2D _rigidBody;
        public float power;
        public Vector2 direction;
        public float duration;

        // Start is called before the first frame update
        void Start()
        {

            StartCoroutine(Timer());
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        IEnumerator Timer()
            {
                yield return new WaitForSeconds(0.05f);

                    if(transform.localScale.x < 0){
                        _rigidBody.AddForce(direction*power*-1, ForceMode2D.Impulse);
                    }else{
                        _rigidBody.AddForce(direction*power, ForceMode2D.Impulse);
                    }

                yield return new WaitForSeconds(duration);
                Destroy(gameObject);
            }

    }
}