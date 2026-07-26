using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TealFalconEnemySeries{

    public class DarkKnightController : MonoBehaviour
    {


        //Movement Settings
        public float currentSpeed = 0f;
        public float animationSpeed = 2f;
        public float acceleration = 4.8f;
        public float MaxWalkSpeed = 2f;
        public float MaxRunSpeed = 7f;
        public float BackStepPower = 200f;
        public bool movingRight = true;

        //Components
        private Animator _animator = null;
        private Rigidbody2D _rigidBody = null;  
        public Material _matRef = null;
        private Material instanceMaterial = null;
        private Vector3 deathPlace;
        private MaterialPropertyBlock mpb;

        //Config
        public bool block = false;

        public enum MovementState {
            Idle,
            Walking,
            Running,
        }

        public enum FightingState {
            OnGuard,
            Attacking,
            Hurt,
            Death,
            Move,
            Idle
        }


        public MovementState CurrentMovementState = MovementState.Idle;
        public FightingState CurrentFightingState = FightingState.Idle;

        public UnityEvent OnHurt,OnDeath, OnCharged;
        
        //Color Settings
        public Color GlowColor;
        

        //Death Settings
        public Color DissolveColor;
        public GameObject ExplosionEffect;
        public Transform ExplosionRef;
        public float DissolveSpeed = 1f;
        private float DissolveStatus = 1f;
        public bool destroy = false;

        public float power = 10.0f;        // Explosion Power

        private Transform BeamAttackRef = null;
        public GameObject DarkBall;


        public List<SpriteRenderer> SRList = null;
        public List<Rigidbody2D>    RBList = null;

        public AudioSource _Channel = null;
        public AudioClip BeamSound, DeathExplosionSound, FootStepSound, PainSound, PowerLoadSound, SwordSound;

        //FootStep 

        private float stepTimer = 0f;
        public float baseStepSpeed = 3f;
        public float minStepSpeed = 0.1f;



        void Awake()
        {
           SetComponents();

        }


        void OnEnable()
        {
           SetComponents();
        }

        void SetComponents(){

            if(_animator == null)
                _animator = transform.Find("Root").GetComponent<Animator>();

            if(_rigidBody == null)
                _rigidBody = GetComponent<Rigidbody2D>();

            if(_matRef == null)
                Debug.LogWarning("NO Material Ref Setted!!!");

            if(SRList == null || SRList.Count == 0)
                SRList = GetAllSpriteRenderersInChildren(transform);

            if(RBList == null || RBList.Count == 0)
                RBList = GetAllRigidbodiesInChildren(transform);
            
            if(BeamAttackRef == null)
                BeamAttackRef = transform.Find("Root/Head_Pivot/BeamAttackRef");


            if(IsBuiltIn(_matRef))
                return;

            //SET MATERIAL OF PRESET
            mpb = new MaterialPropertyBlock();

            if(_matRef.GetTexture("_MainTex") != null)
                mpb.SetTexture("_MainTex", _matRef.GetTexture("_MainTex"));

            mpb.SetTexture("_MagentaPNG",_matRef.GetTexture("_MagentaPNG"));
            mpb.SetTexture("_NormalMap",_matRef.GetTexture("_NormalMap"));
            mpb.SetTexture("_Emission",_matRef.GetTexture("_Emission"));
            mpb.SetFloat("_DissolveScale",_matRef.GetFloat("_DissolveScale"));
            mpb.SetColor("_Glow",GlowColor);
            mpb.SetColor("_DissolveColor",DissolveColor);

           
            ApplyChanges();

        }

        private void ApplyChanges(){
               if(IsBuiltIn(_matRef))
                return;

                foreach (SpriteRenderer sr in SRList)
                {
                    sr.SetPropertyBlock(mpb);
                }
        }

        void Update(){

            if(CurrentFightingState == FightingState.Death){
                DissolveStatus = Mathf.MoveTowards(DissolveStatus, 0f, DissolveSpeed * Time.deltaTime);

                if(!IsBuiltIn(_matRef))
                    mpb.SetFloat("_Dissolve",DissolveStatus);

                ApplyChanges();

                if(DissolveStatus == 0 && destroy)
                    Destroy(gameObject);
            }

            if(CurrentFightingState != FightingState.Idle)
                return;

                float targetSpeed = 0f;

            //Determine Target Speed
            switch(CurrentMovementState) {
                case MovementState.Idle:
                    targetSpeed = 0f;
                    break;
                case MovementState.Walking:
                    targetSpeed = MaxWalkSpeed;
                    break;
                case MovementState.Running:
                    targetSpeed = MaxRunSpeed;
                    break;
            }

        
            // Adjust the current speed based on acceleration
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed * transform.localScale.x, acceleration * Time.deltaTime);

            _animator.SetFloat("Speed",Mathf.Abs(currentSpeed)/animationSpeed);
            
            // Apply the calculated speed to the Rigidbody2D
            _rigidBody.linearVelocity = new Vector2(currentSpeed  , 0);

            //Footstep Sound

            float stepSpeed = Mathf.Lerp(baseStepSpeed, minStepSpeed, Mathf.Abs(currentSpeed) / MaxRunSpeed);
            
            stepTimer += Time.deltaTime;


            if(stepTimer >= stepSpeed && Mathf.Abs(currentSpeed) > 0.1f){
                
    
                PlaySound(FootStepSound);
                
                stepTimer = 0f;
            }



        }

        //Change MovementState to Run
        public void ActivateRun(){

            if(CurrentFightingState != FightingState.Idle)
                return;

            CurrentMovementState = MovementState.Running;

            _animator.SetBool("Busy",false);

        }

        //Change MovementState to Idle
        public void ActivateIdle(){

            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Idle;
            
        }

        //Change MovementState to Walk
        public void ActivateWalk(){

            if(CurrentFightingState != FightingState.Idle)
                return;

            CurrentMovementState = MovementState.Walking;


            _animator.SetBool("Busy",false);

        }

        //Change MovementState to Idle-OnGuard
        public void ActivateGuard(){

            if(CurrentFightingState == FightingState.OnGuard){
                CurrentMovementState = MovementState.Idle;
                CurrentFightingState = FightingState.Idle;
                _animator.SetBool("Guard",false);

            }else{
                CurrentMovementState = MovementState.Idle;
                CurrentFightingState = FightingState.OnGuard;
                _animator.SetBool("Guard",true);
            }
            
        }


        
        //On Guard, Do a Back Step
        public void ActivateBackStep(){       

            if(CurrentFightingState != FightingState.OnGuard)
                return;

            _rigidBody.AddForce(transform.localScale.x*Vector2.left * BackStepPower, ForceMode2D.Impulse);
            _animator.SetTrigger("BackStep");

        }

        //Exec Attack
        public void ActivateAttack(){

            if(CurrentFightingState != FightingState.OnGuard)
                return;

            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Attacking;
            _rigidBody.AddForce(transform.localScale.x*Vector2.right * BackStepPower, ForceMode2D.Impulse);
            
            if(currentSpeed != 0)
                return;

            StartCoroutine(AttackRoutine());

        }

        //Exec Beam Attack
        public void ActivateBeamAttack(){

            if(CurrentFightingState == FightingState.OnGuard){
                ActivateGuard();
            }


            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Attacking;
                
  
            _animator.SetBool("Busy",true);
                        
            if(currentSpeed <= 0f){
                _animator.SetTrigger("BeamAttack");
            }

            StartCoroutine(BeamShootRoutine());

        }

        //Exec Hurt
        public void ActivateHurt(){
            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Hurt;
            StartCoroutine(OnHurtRoutine());
            OnHurt.Invoke();

            PlaySound(PainSound);


        }

        //Exec Death and calls Event Destroy.
        public void ActivateDeath(){
            CurrentFightingState = FightingState.Death;
            _animator.enabled = false;
            deathPlace = transform.position;
            OnDeath.Invoke();

            PlaySound(DeathExplosionSound);

        }

        public void Explode()
        {
            // Explosion Effect
            if (ExplosionEffect != null)
            {
                Instantiate(ExplosionEffect, transform.position, Quaternion.identity);
            }

        
            foreach (Rigidbody2D rb in RBList)
            {
     
                if (rb != null)
                {
                    // Calculate Direction
                   Vector2 direction = new Vector2(rb.transform.position.x - ExplosionRef.position.x,rb.transform.position.y-ExplosionRef.position.y).normalized;
            
                    // Apply Force
                    rb.gravityScale = 0f;
                    rb.AddForce(direction * power, ForceMode2D.Impulse);

                }
            }
    }

        public void ResetState(){

            if(DissolveStatus > 0)
                return;

            _animator.enabled = true;       
            DissolveStatus = 1f;
            instanceMaterial.SetFloat("_Dissolve",DissolveStatus);
            transform.position = deathPlace;
            _rigidBody.linearVelocity = Vector2.zero;
            _rigidBody.angularVelocity = 0f;    
            
            foreach (Rigidbody2D rb in RBList)
            {
     
                   // rb.gravityScale = 1f;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;    
            
            }

            CurrentMovementState = MovementState.Idle;
            CurrentFightingState = FightingState.Idle;

        }

        public void Flip()
        {
            // Flip the enemy sprite by inverting the x scale
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
            currentSpeed = currentSpeed * -1;

        }




        IEnumerator AttackRoutine()
        {
            // Activar la animación de ataque
            _animator.SetTrigger("Attack");

            
            PlaySound(SwordSound);

            // Esperar hasta que termine la animación de ataque
            while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            {
                yield return null;
            }

            // Esperar hasta que termine la animación de ataque completamente
            while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.3f)
            {
                yield return null;
            }

            // Volver al estado de guardia después del ataque
            CurrentFightingState = FightingState.OnGuard;
        }

        IEnumerator OnHurtRoutine()
        {
            // Activate Hurt Animation
            _animator.SetTrigger("Hurt");

            // Waiting till Hurt animation is finished.
            while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Hurt"))
            {
                yield return null;
            }

            // Waiting time minimum
            while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            {
                yield return null;
            }

            _animator.SetBool("Busy",false);

            // Back to Guard
            CurrentFightingState = FightingState.OnGuard;
            CurrentMovementState = MovementState.Idle;

        }


        IEnumerator BeamShootRoutine()
        {

            while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("BeamAttack"))
            {
                yield return null;
            }

                PlaySound(PowerLoadSound);


            yield return new WaitForSeconds(2.4f);
            
                PlaySound(BeamSound);

            // Shot Ball!!
            if (DarkBall != null)
            {
                GameObject Ball = Instantiate(DarkBall, BeamAttackRef.position, Quaternion.identity);
                Ball.transform.localScale = transform.localScale;
                Vector3 localScale = transform.localScale;

            }
            _animator.SetBool("Busy",false);


        }


        public List<Rigidbody2D> GetAllRigidbodiesInChildren(Transform parent)
        {
            List<Rigidbody2D> rigidbodies = new List<Rigidbody2D>(); 

            Rigidbody2D[] rbArray = parent.GetComponentsInChildren<Rigidbody2D>();

            foreach (Rigidbody2D rb in rbArray)
            {
                rigidbodies.Add(rb);
            }

            return rigidbodies; 
        }    
        
      public List<SpriteRenderer> GetAllSpriteRenderersInChildren(Transform parent)
        {
            List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>(); 

            SpriteRenderer[] srArray = parent.GetComponentsInChildren<SpriteRenderer>();


            foreach (SpriteRenderer sr in srArray)
            {
                spriteRenderers.Add(sr);
            }

            return spriteRenderers; 
        }

        private void PlaySound(AudioClip _clip){
            
            if(_clip == null){
                Debug.LogWarning("Sound not setted.");
                return;
            }
        
            if(_Channel == null){
                Debug.LogWarning("Audio Source not setted.");
                return;
            }

            _Channel.PlayOneShot(_clip);

        }

        // Is Built In?
        public bool IsBuiltIn(Material material)
        {
            return material.shader.name != "DarkKnight/DarkKnightShader";
        }
    }
}