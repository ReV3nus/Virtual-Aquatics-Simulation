using UnityEditor;
using UnityEngine;

namespace Editor.ShaderGUI
{
    public class CustomShaderGUI : UnityEditor.ShaderGUI
    {
        enum SpecularChoice
        {
            True,
            False
        }

        enum ShaderTypeChoice
        {
            NormalOnly,
            BlinnPhong
        }

        MaterialEditor editor;
        MaterialProperty[] properties;
        Material target;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
        {
            editor = materialEditor;
            properties = materialProperties;
            target = editor.target as Material;

            ShaderTypeChoice shaderTypeChoice = ShaderTypeChoice.BlinnPhong;
            if (target.IsKeywordEnabled("NORMAL_ONLY"))
                shaderTypeChoice = ShaderTypeChoice.NormalOnly;
            EditorGUI.BeginChangeCheck();
            shaderTypeChoice = (ShaderTypeChoice)EditorGUILayout.EnumPopup(
                new GUIContent("Shader Type"), shaderTypeChoice);
            if (EditorGUI.EndChangeCheck())
            {
                if (shaderTypeChoice == ShaderTypeChoice.NormalOnly)
                    target.EnableKeyword("NORMAL_ONLY");
                else
                    target.DisableKeyword("NORMAL_ONLY");
            }

            if (shaderTypeChoice == ShaderTypeChoice.BlinnPhong)
            {
                MaterialProperty mainTex = FindProperty("_MainTex", properties);
                GUIContent mainTexLabel = new GUIContent(mainTex.displayName);
                editor.TextureProperty(mainTex, mainTexLabel.text);

                SpecularChoice specularChoice = SpecularChoice.False;
                if (target.IsKeywordEnabled("USE_SPECULAR"))
                    specularChoice = SpecularChoice.True;
                EditorGUI.BeginChangeCheck();
                specularChoice = (SpecularChoice)EditorGUILayout.EnumPopup(
                    new GUIContent("Use Specular?"), specularChoice
                );
                if (EditorGUI.EndChangeCheck())
                {
                    if (specularChoice == SpecularChoice.True)
                        target.EnableKeyword("USE_SPECULAR");
                    else
                        target.DisableKeyword("USE_SPECULAR");
                }

                if (specularChoice == SpecularChoice.True)
                {
                    MaterialProperty shininess = FindProperty("_Shininess", properties);
                    GUIContent shininessLabel = new GUIContent(shininess.displayName);
                    editor.RangeProperty(shininess, "Specular Factor");
                }
            }
        }
    }
}