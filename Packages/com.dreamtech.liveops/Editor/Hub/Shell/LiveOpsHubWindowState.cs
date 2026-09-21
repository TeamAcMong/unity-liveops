using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Trạng thái cửa sổ sống qua domain reload (8.10, [FD §4.1] khung 3): màn đang mở, outcome gần nhất, trạng thái view của
    /// từng màn (JSON do màn tự dựng), câu "Vừa làm". Lớp <c>[Serializable]</c> nằm trong field <c>[SerializeField]</c> của
    /// <see cref="LiveOpsHubWindow"/> — Unity serialize EditorWindow trước khi reload và dựng lại sau, nên sửa script giữa lúc
    /// đang chọn một đợt không làm mất lựa chọn, zoom, pane đang mở. <c>viewDataKey</c> không dùng được cho việc này vì chỉ
    /// lưu control dựng sẵn.
    /// </summary>
    [Serializable]
    internal sealed class LiveOpsHubWindowState
    {
        [SerializeField] private string activeSectionId = string.Empty;
        [SerializeField] private LiveOpsOutcomeRecord lastOutcome;
        [SerializeField] private List<SectionViewStateEntry> sectionViewStates = new List<SectionViewStateEntry>();
        [SerializeField] private string recentActionText = string.Empty;
        [SerializeField] private int recentActionUndoGroup = LiveOpsToastModel.NoUndoGroup;

        public string ActiveSectionId
        {
            get => activeSectionId ?? string.Empty;
            set => activeSectionId = value ?? string.Empty;
        }

        /// <summary>
        /// null khi chưa có outcome. Serializer của Unity dựng field lớp [Serializable] thành instance rỗng thay vì null, nên
        /// "không có" được nhận ra bằng dòng chính rỗng — outcome thật luôn có dòng chính.
        /// </summary>
        public LiveOpsOutcomeRecord LastOutcome
        {
            get => lastOutcome != null && !string.IsNullOrEmpty(lastOutcome.Headline) ? lastOutcome : null;
            set => lastOutcome = value;
        }

        public string RecentActionText
        {
            get => recentActionText ?? string.Empty;
            set => recentActionText = value ?? string.Empty;
        }

        public int RecentActionUndoGroup
        {
            get => recentActionUndoGroup;
            set => recentActionUndoGroup = value;
        }

        /// <summary>Số màn đang có trạng thái view đã lưu.</summary>
        public int SectionViewStateCount => sectionViewStates == null ? 0 : sectionViewStates.Count;

        /// <summary>"" khi màn chưa lưu gì — màn nhận "" thì dựng trạng thái mặc định.</summary>
        public string GetSectionViewState(string sectionId)
        {
            SectionViewStateEntry entry = Find(sectionId);
            return entry == null ? string.Empty : entry.ViewStateJson;
        }

        /// <summary>Ghi đè trạng thái của một màn; JSON rỗng/null xoá mục (không để mục rác của màn đã bỏ lưu).</summary>
        public void SetSectionViewState(string sectionId, string viewStateJson)
        {
            if (string.IsNullOrEmpty(sectionId)) throw new ArgumentNullException(nameof(sectionId));
            if (sectionViewStates == null) sectionViewStates = new List<SectionViewStateEntry>();
            SectionViewStateEntry entry = Find(sectionId);
            if (string.IsNullOrEmpty(viewStateJson))
            {
                if (entry != null) sectionViewStates.Remove(entry);
                return;
            }
            if (entry == null)
            {
                sectionViewStates.Add(new SectionViewStateEntry(sectionId, viewStateJson));
                return;
            }
            entry.ViewStateJson = viewStateJson;
        }

        private SectionViewStateEntry Find(string sectionId)
        {
            if (sectionViewStates == null || string.IsNullOrEmpty(sectionId)) return null;
            foreach (SectionViewStateEntry entry in sectionViewStates)
            {
                if (entry != null && string.Equals(entry.SectionId, sectionId, StringComparison.Ordinal)) return entry;
            }
            return null;
        }

        /// <summary>Một cặp (id màn, JSON) — danh sách thay cho Dictionary vì serializer của Unity không lưu Dictionary.</summary>
        [Serializable]
        internal sealed class SectionViewStateEntry
        {
            [SerializeField] private string sectionId;
            [SerializeField] private string viewStateJson;

            // Serializer của Unity cần hàm dựng không tham số.
            private SectionViewStateEntry()
            {
            }

            public SectionViewStateEntry(string sectionId, string viewStateJson)
            {
                this.sectionId = sectionId ?? string.Empty;
                this.viewStateJson = viewStateJson ?? string.Empty;
            }

            public string SectionId => sectionId ?? string.Empty;

            public string ViewStateJson
            {
                get => viewStateJson ?? string.Empty;
                set => viewStateJson = value ?? string.Empty;
            }
        }
    }
}
