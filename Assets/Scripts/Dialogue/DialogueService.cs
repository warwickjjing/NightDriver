using System;
using NightDriver.Core;
using UnityEngine;
using Yarn.Unity;

namespace NightDriver.Dialogue
{
    /// <summary>
    /// Yarn Spinner 대화 실행의 단일 진입점(씬당 1개 권장).
    ///
    /// 다른 시스템은 Yarn의 세부 컴포넌트를 직접 참조하지 않고,
    /// 이 컴포넌트만 통해 대화를 시작/종료/상태 확인합니다.
    /// </summary>
    public sealed class DialogueService : SingletonBehaviour<DialogueService>
    {
        public event Action<string> OnDialogueStarted;
        public event Action OnDialogueCompleted;

        [Header("Yarn")]
        [SerializeField] private DialogueRunner runner;
        [Tooltip("비워두면 DialogueRunner가 가진 VariableStorage를 자동 사용합니다.")]
        [SerializeField] private VariableStorageBehaviour variables;

        public DialogueRunner Runner => runner;
        public VariableStorageBehaviour Variables => variables != null ? variables : runner != null ? runner.VariableStorage : null;
        public bool IsRunning => runner != null && runner.IsDialogueRunning;

        // ─────────────────────────────────────────────

        protected override bool PersistAcrossScenes => true;

        protected override void OnInitialize()
        {
            if (runner == null)    runner    = GetComponentInChildren<DialogueRunner>(true);
            if (variables == null && runner != null) variables = runner.VariableStorage;
        }

        private void OnEnable()
        {
            if (runner != null) runner.onDialogueComplete.AddListener(HandleDialogueComplete);
        }

        private void OnDisable()
        {
            if (runner != null) runner.onDialogueComplete.RemoveListener(HandleDialogueComplete);
        }

        // ─────────────────────────────────────────────

        /// <summary>
        /// 지정된 Yarn 노드로 대화를 시작합니다.
        /// 이미 대화가 진행 중이거나 노드명이 비어있으면 false를 반환합니다.
        /// </summary>
        public bool TryStart(string startNode)
        {
            if (runner == null) return false;
            if (string.IsNullOrWhiteSpace(startNode)) return false;
            if (runner.IsDialogueRunning) return false;

            OnDialogueStarted?.Invoke(startNode);
            runner.StartDialogue(startNode);
            return true;
        }

        /// <summary>진행 중인 대화를 강제 종료합니다.</summary>
        public void Stop()
        {
            if (runner == null || !runner.IsDialogueRunning) return;
            runner.Stop();
        }

        // ─────────────────────────────────────────────
        // 변수 설정 헬퍼

        public void SetBool(string name, bool value)   => Variables?.SetValue($"${name}", value);
        public void SetNumber(string name, float value) => Variables?.SetValue($"${name}", value);
        public void SetString(string name, string value) => Variables?.SetValue($"${name}", value);

        // ─────────────────────────────────────────────

        private void HandleDialogueComplete() => OnDialogueCompleted?.Invoke();
    }
}
