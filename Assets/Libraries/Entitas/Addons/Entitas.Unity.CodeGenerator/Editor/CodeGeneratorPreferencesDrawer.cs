using System;
using System.Collections.Generic;
using System.Linq;
using Entitas.CodeGenerator;
using UnityEditor;
using UnityEngine;

namespace Entitas.Unity.CodeGenerator {

    public class CodeGeneratorPreferencesDrawer : IEntitasPreferencesDrawer {

        public int priority { get { return 10; } }

        string[] _availableDataProviderTypes;
        string[] _availableGeneratorTypes;
        string[] _availablePostProcessorTypes;

        string[] _availableDataProviderNames;
        string[] _availableGeneratorNames;
        string[] _availablePostProcessorNames;

        CodeGeneratorConfig _codeGeneratorConfig;
        List<string> _contexts;
        UnityEditorInternal.ReorderableList _contextList;

        public void Initialize(EntitasPreferencesConfig config) {
            var enabledDataProviderNames = initPhase<ICodeGeneratorDataProvider>(out _availableDataProviderTypes, out _availableDataProviderNames);
            var enabledGeneratorNames = initPhase<ICodeGenerator>(out _availableGeneratorTypes, out _availableGeneratorNames);
            var enabledPostProcessorNames = initPhase<ICodeGenFilePostProcessor>(out _availablePostProcessorTypes, out _availablePostProcessorNames);

			_codeGeneratorConfig = new CodeGeneratorConfig(config, enabledDataProviderNames, enabledGeneratorNames, enabledPostProcessorNames);

            _contexts = new List<string>(_codeGeneratorConfig.contexts);

            _contextList = new UnityEditorInternal.ReorderableList(_contexts, typeof(string), true, true, true, true);
            _contextList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Contexts");
            _contextList.drawElementCallback = (rect, index, isActive, isFocused) => {
                rect.width -= 20;
                _contexts[index] = EditorGUI.TextField(rect, _contexts[index]);
            };
            _contextList.onAddCallback = list => list.list.Add("New Context");
            _contextList.onCanRemoveCallback = list => list.count > 1;
            _contextList.onChangedCallback = list => GUI.changed = true;
        }

        public void Draw(EntitasPreferencesConfig config) {
            EntitasEditorLayout.BeginVerticalBox();
            {
                EditorGUILayout.LabelField("Code Generator", EditorStyles.boldLabel);

                drawGeneratedFolderPath();
                drawContexts();

                _codeGeneratorConfig.dataProviders = drawMaskField("Data Providers", _availableDataProviderTypes, _availableDataProviderNames, _codeGeneratorConfig.dataProviders);
                _codeGeneratorConfig.codeGenerators = drawMaskField("Code Generators", _availableGeneratorTypes, _availableGeneratorNames, _codeGeneratorConfig.codeGenerators);
                _codeGeneratorConfig.postProcessors = drawMaskField("Post Processors", _availablePostProcessorTypes, _availablePostProcessorNames, _codeGeneratorConfig.postProcessors);

                var bgColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                if(GUILayout.Button("Generate", GUILayout.Height(32))) {
                    UnityCodeGenerator.Generate();
                }
                GUI.backgroundColor = bgColor;
            }
            EntitasEditorLayout.EndVertical();
        }

        void drawGeneratedFolderPath() {
            _codeGeneratorConfig.targetDirectory = EditorGUILayout.TextField("Target Directory", _codeGeneratorConfig.targetDirectory);
        }

        void drawContexts() {
            EditorGUILayout.Space();

            _contextList.DoLayoutList();

            if(_contexts.Count <= 1) {
                EditorGUILayout.HelpBox("You can optimize the memory footprint of entities by creating multiple contexts. " +
                "The code generator generates subclasses of ContextAttribute for each context name. " +
                "You have to assign components to one or more contexts with the generated attribute, e.g. [Game] or [Input], " +
                "otherwise they will be ignored by the code generator.", MessageType.Info);
            }

            _codeGeneratorConfig.contexts = _contexts.ToArray();
        }

        static string[] initPhase<T>(out string[] availableTypes, out string[] availableNames) where T : ICodeGeneratorInterface {
            var instances = UnityCodeGenerator
                .GetTypes<T>()
                .Where(type => !type.IsAbstract)
                .Select(type => (T)Activator.CreateInstance(type))
                .OrderBy(instance => instance.name);

            availableTypes = instances
                .Select(instance => instance.GetType().Name)
                .ToArray();

            availableNames = instances
                .Select(instance => instance.name)
                .ToArray();

            return instances
                .Where(instance => instance.isEnabledByDefault)
                .Select(instance => instance.GetType().Name)
                .ToArray();
        }

        static string[] drawMaskField(string title, string[] types, string[] names, string[] input) {
            var mask = 0;

            for(int i = 0; i < types.Length; i++) {
                if(input.Contains(types[i])) {
                    mask += (1 << i);
                }
            }

            mask = EditorGUILayout.MaskField(title, mask, names);

            var selected = new List<string>();
            for(int i = 0; i < types.Length; i++) {
                var index = 1 << i;
                if((index & mask) == index) {
                    selected.Add(types[i]);
                }
            }

            return selected.ToArray();
        }
    }
}
