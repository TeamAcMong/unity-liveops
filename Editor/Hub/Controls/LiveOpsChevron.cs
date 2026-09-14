using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Mũi tên gập / điều hướng / dấu cắt mép vẽ bằng VisualElement 5×5 viền trên + phải 1,5px xoay bằng class
    /// ([FD §2.9]). Không viết ký tự ‹ › ▸ ▾: font Label mặc định (Inter-Regular SDF) thiếu ▾ ▸ ở cả hai bản Unity
    /// ([API §12.3]) nên ký tự sẽ thành ô trống. Hướng là class chứ không phải style.rotate vì [FD §2.14] chỉ cho C# gán
    /// hình học suy từ dữ liệu.
    /// </summary>
    internal sealed class LiveOpsChevron : VisualElement
    {
        internal enum ChevronDirection
        {
            Right = 0,
            Left = 1,
            Down = 2,
            Up = 3,
        }

        private static readonly string[] DirectionClasses =
        {
            LiveOpsHubClassNames.ChevronRight, LiveOpsHubClassNames.ChevronLeft, LiveOpsHubClassNames.ChevronDown, LiveOpsHubClassNames.ChevronUp,
        };

        private ChevronDirection _direction;

        public LiveOpsChevron() : this(ChevronDirection.Right)
        {
        }

        public LiveOpsChevron(ChevronDirection direction)
        {
            // Mũi tên chỉ là hình trang trí đi kèm chữ/nút — không chặn click của hàng hay chip chứa nó.
            pickingMode = PickingMode.Ignore;
            AddToClassList(LiveOpsHubClassNames.Chevron);
            Direction = direction;
        }

        public ChevronDirection Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                foreach (string className in DirectionClasses) RemoveFromClassList(className);
                AddToClassList(ClassOf(value));
            }
        }

        internal static string ClassOf(ChevronDirection direction)
        {
            switch (direction)
            {
                case ChevronDirection.Right: return LiveOpsHubClassNames.ChevronRight;
                case ChevronDirection.Left: return LiveOpsHubClassNames.ChevronLeft;
                case ChevronDirection.Down: return LiveOpsHubClassNames.ChevronDown;
                case ChevronDirection.Up: return LiveOpsHubClassNames.ChevronUp;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}
