using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;

namespace TealFalconEnemySeries {

[CustomEditor(typeof(DarkKnightController))]
public class DarkKnightControllerEditor : Editor
{

    //GENERAL
    private SerializedObject serializedObj;
    private Texture2D image;
    private DarkKnightController _DKControl;

    SerializedProperty CurrentFightingState;
    SerializedProperty CurrentMovementState;

    SerializedProperty ExplosionEffect;
    SerializedProperty ExplosionRef;
    SerializedProperty _matRef;
    SerializedProperty destroy;


    SerializedProperty OnHurt;
    SerializedProperty OnDeath;
    SerializedProperty OnCharged;

    SerializedProperty SRList;
    SerializedProperty RBList;

    //AUDIO CLIPS
    SerializedProperty BeamSound;
    SerializedProperty DeathExplosionSound;
    SerializedProperty FootStepSound;
    SerializedProperty PainSound;
    SerializedProperty PowerLoadSound;
    SerializedProperty SwordSound;

    SerializedProperty _Channel;

    // ------------> FOLDOUTS
    bool showSounds = false;
    bool showDeathSettings = false;
    bool showFightSettings = false;


    SerializedProperty DarkBall;


    void OnEnable()
    {
        // Initialized Serialize Object
        serializedObj = new SerializedObject(target);
        
        //Setting Image
        image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Dark Knight/Scripts/Editor/topbar.png");


        //States
        CurrentFightingState = serializedObj.FindProperty("CurrentFightingState");
        CurrentMovementState = serializedObj.FindProperty("CurrentMovementState");

        //Setting Properties
        _matRef = serializedObj.FindProperty("_matRef");
        ExplosionEffect = serializedObj.FindProperty("ExplosionEffect");
        ExplosionRef = serializedObj.FindProperty("ExplosionRef");
        OnHurt = serializedObj.FindProperty("OnHurt");
        OnDeath = serializedObj.FindProperty("OnDeath");
        OnCharged = serializedObj.FindProperty("OnCharged");
        DarkBall = serializedObj.FindProperty("DarkBall");
        destroy = serializedObj.FindProperty("destroy");

        SRList = serializedObj.FindProperty("SRList");
        RBList = serializedObj.FindProperty("RBList");

        //SOUNDS
        BeamSound = serializedObj.FindProperty("BeamSound");
        DeathExplosionSound = serializedObj.FindProperty("DeathExplosionSound");
        FootStepSound = serializedObj.FindProperty("FootStepSound");
        PainSound = serializedObj.FindProperty("PainSound");
        PowerLoadSound = serializedObj.FindProperty("PowerLoadSound");
        SwordSound = serializedObj.FindProperty("SwordSound");

        _Channel = serializedObj.FindProperty("_Channel"); 
    }


    public override void OnInspectorGUI(){


        serializedObj.Update();
        _DKControl = (DarkKnightController) target;

       // Crear un estilo de GUI personalizado con un margen cero
        GUIStyle imageStyle = new GUIStyle(GUI.skin.label);
        imageStyle.margin = new RectOffset(0, 0, 0, 0);

        // Dibujar la imagen utilizando el nuevo estilo
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(new GUIContent(image), GUILayout.Width(420), GUILayout.Height(80));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
        //VALUES

        GUI.enabled = false;

        EditorGUILayout.PropertyField(CurrentFightingState, new GUIContent("Fight State"));
        EditorGUILayout.PropertyField(CurrentMovementState, new GUIContent("Movement State"));
        
        GUI.enabled = true;

        EditorGUILayout.PropertyField(_matRef, new GUIContent("Material"));
        ColorFrameHDR("Eye Color ", ref _DKControl.GlowColor);


        SliderFloatValue("Speed",-15f,15f,ref _DKControl.currentSpeed);
        SliderFloatValue("Acceleration",1f,15f,ref _DKControl.acceleration);

        SliderFloatValue("Max Walking Speed",0.5f,3f,ref _DKControl.MaxWalkSpeed);
        SliderFloatValue("Max Running Speed",3.1f,15f,ref _DKControl.MaxRunSpeed);

        EditorGUILayout.LabelField("FootSteps", EditorStyles.boldLabel);

        SliderFloatValue("Base Step Speed",1f,6f,ref _DKControl.baseStepSpeed);
        SliderFloatValue("Min Step Speed",0f,1f,ref _DKControl.minStepSpeed);

        EditorGUILayout.LabelField("Commands", EditorStyles.boldLabel);
            Commands(_DKControl);
       

        showDeathSettings = EditorGUILayout.Foldout(showDeathSettings, "Death Settings");

        if (showDeathSettings)
        {
            DeathSettings(_DKControl);
        } 
        
        showFightSettings = EditorGUILayout.Foldout(showFightSettings, "Fight Settings");

        if (showFightSettings)
        {
            FightSettings(_DKControl);
        }

        showSounds = EditorGUILayout.Foldout(showSounds, "Sounds");
        if(showSounds){
            Sounds();
        }
        serializedObj.ApplyModifiedProperties();

       if (GUI.changed)
        {
            EditorUtility.SetDirty(_DKControl);
            PrefabUtility.RecordPrefabInstancePropertyModifications(_DKControl);
        }
    }

    private void Sounds(){
        
        
        EditorGUILayout.PropertyField(_Channel, new GUIContent("AudioSource/Channel")); 

        EditorGUILayout.PropertyField(FootStepSound, new GUIContent("FootStep Sound")); 
        EditorGUILayout.PropertyField(SwordSound, new GUIContent("Attack Sound")); 
        EditorGUILayout.PropertyField(PainSound, new GUIContent("Pain Sound")); 
        EditorGUILayout.PropertyField(PowerLoadSound, new GUIContent("PowerLoad Sound")); 
        EditorGUILayout.PropertyField(BeamSound, new GUIContent("Beam Sound")); 
        EditorGUILayout.PropertyField(DeathExplosionSound, new GUIContent("DeathExplosion Sound")); 

    }



    private void DeathSettings(DarkKnightController control){

        // Death Settings

        EditorGUILayout.PropertyField(destroy, new GUIContent("Destroy Gameobject")); 
        EditorGUILayout.PropertyField(ExplosionEffect, new GUIContent("Explosion Prefab")); 
        EditorGUILayout.PropertyField(ExplosionRef, new GUIContent("Explosion Center"));       
        SliderFloatValue("Explosion Power",-300f,300f,ref _DKControl.power);


        EditorGUILayout.PropertyField(SRList, new GUIContent("SpriteRenderers"));
        EditorGUILayout.PropertyField(RBList, new GUIContent("Rigidbodies"));

        ColorFrameHDR("Dissolve Color ", ref _DKControl.DissolveColor);
        SliderFloatValue("Dissolve Time",0.05f,3f,ref _DKControl.DissolveSpeed);
        EditorGUILayout.PropertyField(OnDeath, new GUIContent("Death Event"));



    }


  private void FightSettings(DarkKnightController control){

        //Fight Settings

        SliderFloatValue("Step Power",0f,600f,ref _DKControl.BackStepPower);

        EditorGUILayout.PropertyField(DarkBall, new GUIContent("Beam Attack Object"));

        EditorGUILayout.PropertyField(OnCharged, new GUIContent("Beam Charged Event"));

        EditorGUILayout.PropertyField(OnHurt, new GUIContent("Hurt Event"));

    }


    private void Commands(DarkKnightController control){

        if (Application.isPlaying){
        
        CenteredLabel("Set Movement State");

         EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Idle"))
                {
                    control.ActivateIdle();
                }

                if (GUILayout.Button("Walk"))
                {
                    control.ActivateWalk();
                }

                if (GUILayout.Button("Run"))
                {
                    control.ActivateRun();
                }

                if (GUILayout.Button("Flip!"))
                {
                    control.Flip();
                }

                if (GUILayout.Button("Reset"))
                {
                    control.ResetState();
                }
            
            EditorGUILayout.EndHorizontal();
          


       CenteredLabel("Actions");

         EditorGUILayout.BeginHorizontal();

           if (GUILayout.Button("Hurt"))
            {
                control.ActivateHurt();
            }

            if (GUILayout.Button("Death"))
            {
                control.ActivateDeath();
            }
          
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Attack"))
            {
                control.ActivateAttack();
            }

            if (GUILayout.Button("Guard"))
            {
                control.ActivateGuard();
            }
              
            if (GUILayout.Button("Back Step"))
            {
                control.ActivateBackStep();
            }
            
            EditorGUILayout.EndHorizontal();
           
            if (GUILayout.Button("Beam!!!"))
            {
                control.ActivateBeamAttack();
            }

        }
    }


    private void CenteredLabel(string Label){
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(Label, EditorStyles.label);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void SliderFloatValue(string Label, float min, float max, ref float value){
        float sliderValue = EditorGUILayout.Slider(Label, value, min, max);
        value = sliderValue;
    }
 
    private void ColorFrameHDR(string Label, ref Color value){

        Color selectedReadColor = value;    
        Color selectedColor = EditorGUILayout.ColorField(new GUIContent(Label), selectedReadColor, true, true, true);
        value = new Vector4(selectedColor.r, selectedColor.g, selectedColor.b, selectedColor.a);
    }

    private void Vector2Frame(string Label, ref float xValue, ref float yValue){
        Vector2 vectorValue = EditorGUILayout.Vector2Field(Label, new Vector2(xValue, yValue));  
        xValue = vectorValue.x;
        yValue = vectorValue.y; 
    }

    private void TextureFrame(string Label , ref Texture2D value){
        Texture2D textureField = EditorGUILayout.ObjectField(Label, value, typeof(Texture2D), false) as Texture2D;
        value = textureField;
    }

  }

}