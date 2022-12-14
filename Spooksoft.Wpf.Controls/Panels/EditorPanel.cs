using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Spooksoft.Wpf.Controls.Panels
{
    public class EditorPanel : Panel
    {
        private enum GeneralAlignment
        {
            Begin,
            Center,
            End,
            Stretch
        }

        private static GeneralAlignment ToGeneralAlignment(VerticalAlignment verticalAlignment)
        {
            switch (verticalAlignment)
            {
                case VerticalAlignment.Top:
                    return GeneralAlignment.Begin;
                case VerticalAlignment.Center:
                    return GeneralAlignment.Center;
                case VerticalAlignment.Bottom:
                    return GeneralAlignment.End;
                case VerticalAlignment.Stretch:
                    return GeneralAlignment.Stretch;
                default:
                    throw new InvalidEnumArgumentException("Unsupported vertical alignment!");
            }
        }

        private static GeneralAlignment ToGeneralAlignment(HorizontalAlignment horizontalAlignment)
        {
            switch (horizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    return GeneralAlignment.Begin;
                case HorizontalAlignment.Center:
                    return GeneralAlignment.Center;
                case HorizontalAlignment.Right:
                    return GeneralAlignment.End;
                case HorizontalAlignment.Stretch:
                    return GeneralAlignment.Stretch;
                default:
                    throw new InvalidEnumArgumentException("Unsupported horizontal alignment!");
            }
        }

        private Size DesiredSizeWithMargin(UIElement element)
        {
            if (element == null)
                return Size.Empty;

            if (element is FrameworkElement frameworkElement)
                return new Size(frameworkElement.DesiredSize.Width + frameworkElement.Margin.Left + frameworkElement.Margin.Right,
                  frameworkElement.DesiredSize.Height + frameworkElement.Margin.Top + frameworkElement.Margin.Bottom);
            else
                return element.DesiredSize;
        }

        private static (double elementStart, double elementSize) EvalPlacement(UIElement element,
            double placementRectStart,
            double placementRectSize,
            double elementMarginBegin,
            double elementMarginEnd,
            double elementDesiredSize,
            GeneralAlignment elementAlignment)
        {
            double resultSize;
            double resultStart;

            switch (elementAlignment)
            {
                case GeneralAlignment.Begin:
                    resultSize = Math.Max(0, Math.Min(elementDesiredSize, placementRectSize - (elementMarginBegin + elementMarginEnd)));
                    resultStart = placementRectStart + elementMarginBegin;
                    break;

                case GeneralAlignment.Center:
                    resultSize = Math.Max(0, Math.Min(elementDesiredSize, placementRectSize - (elementMarginBegin + elementMarginEnd)));
                    resultStart = placementRectStart + (placementRectSize - (resultSize + elementMarginBegin + elementMarginEnd)) / 2 + elementMarginBegin;
                    break;

                case GeneralAlignment.End:
                    resultSize = Math.Max(0, Math.Min(elementDesiredSize, placementRectSize - (elementMarginBegin + elementMarginEnd)));
                    resultStart = placementRectStart + placementRectSize - elementMarginEnd - resultSize;
                    break;

                case GeneralAlignment.Stretch:
                    resultSize = Math.Max(0, placementRectSize - (elementMarginBegin + elementMarginEnd));
                    resultStart = placementRectStart + elementMarginBegin;
                    break;

                default:
                    throw new InvalidEnumArgumentException("Unsupported alignment!");
            }

            return (resultStart, resultSize);
        }


        private void ArrangeWithAlignment(UIElement element, Rect placementRect, Size cachedDesiredSize)
        {
            if (cachedDesiredSize == Size.Empty)
                cachedDesiredSize = DesiredSizeWithMargin(element);

            Thickness elementMargin = new Thickness();
            HorizontalAlignment elementHorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment elementVerticalAlignment = VerticalAlignment.Top;

            if (element is FrameworkElement frameworkElement)
            {
                elementMargin = frameworkElement.Margin;
                elementHorizontalAlignment = frameworkElement.HorizontalAlignment;
                elementVerticalAlignment = frameworkElement.VerticalAlignment;
            }

            (double elementTop, double elementHeight) = EvalPlacement(element,
                placementRect.Top,
                placementRect.Height,
                elementMargin.Top,
                elementMargin.Bottom,
                cachedDesiredSize.Height,
                ToGeneralAlignment(elementVerticalAlignment));

            (double elementLeft, double elementWidth) = EvalPlacement(element,
                placementRect.Left,
                placementRect.Width,
                elementMargin.Left,
                elementMargin.Right,
                cachedDesiredSize.Width,
                ToGeneralAlignment(elementHorizontalAlignment));

            element.Arrange(new Rect(elementLeft, elementTop, elementWidth, elementHeight));
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Measure labels

            List<Size> labelSizes = new List<Size>();
            for (int i = 0; i < InternalChildren.Count; i += 2)
            {
                // Measure label
                Thickness labelMargin = new Thickness(0);
                if (InternalChildren[i] is FrameworkElement frameworkElement)
                    labelMargin = frameworkElement.Margin;

                InternalChildren[i].Measure(new Size(Math.Max(0, availableSize.Width - (labelMargin.Left + labelMargin.Right)),
                    Math.Max(0, availableSize.Height - (labelMargin.Top + labelMargin.Bottom))));
                labelSizes.Add(DesiredSizeWithMargin(InternalChildren[i]));
            }

            double maxLabelWidth = labelSizes.Max(ls => ls.Width);

            // Measure editors

            List<Size> editorSizes = new List<Size>();
            for (int i = 1; i < InternalChildren.Count; i += 2)
            {
                Thickness editorMargin = new Thickness(0);
                if (InternalChildren[i] is FrameworkElement frameworkElement)
                    editorMargin = frameworkElement.Margin;

                InternalChildren[i].Measure(new Size(Math.Max(0, availableSize.Width - maxLabelWidth - (editorMargin.Left + editorMargin.Right)),
                    Math.Max(0, availableSize.Height - (editorMargin.Top + editorMargin.Bottom))));
                editorSizes.Add(DesiredSizeWithMargin(InternalChildren[i]));
            }

            double maxEditorWidth = editorSizes.Any() ? editorSizes.Max(es => es.Width) : 0;

            // Equalize count

            while (editorSizes.Count < labelSizes.Count)
                editorSizes.Add(Size.Empty);

            // Evaluate total height

            double totalLabelEditorPairHeight = labelSizes.Zip(editorSizes, (first, second) => new { First = first, Second = second })
                .Select(sizes => Math.Max(sizes.First.Height, sizes.Second.Height))
                .Sum();

            // This is required height, regardless of how much space is available
            double resultHeight = totalLabelEditorPairHeight;

            // If space is not constrained, pick as much as labels & editors want. Else, use
            // as much, as is given.
            double resultWidth = Math.Min(availableSize.Width, maxLabelWidth + maxEditorWidth);

            return new Size(resultWidth, resultHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Label area width

            double labelAreaWidth = 0;
            for (int i = 0; i < InternalChildren.Count; i += 2)
                labelAreaWidth = Math.Max(labelAreaWidth, DesiredSizeWithMargin(InternalChildren[i]).Width);

            labelAreaWidth = Math.Min(labelAreaWidth, finalSize.Width);

            // Editor area width

            double editorAreaWidth = Math.Max(0, finalSize.Width - labelAreaWidth);

            // Arranging controls

            double y = 0;
            int controlIndex = 0;

            while (controlIndex < InternalChildren.Count)
            {
                // Retrieve label and editor

                UIElement label = InternalChildren[controlIndex++];
                Size labelDesiredSize = DesiredSizeWithMargin(label);

                UIElement editor = controlIndex < InternalChildren.Count ? InternalChildren[controlIndex++] : null;
                Size editorDesiredSize = DesiredSizeWithMargin(editor);

                double rowHeight = Math.Max(labelDesiredSize.Height, editorDesiredSize.Height);

                var labelArea = new Rect(0, y, labelAreaWidth, rowHeight);
                ArrangeWithAlignment(label, labelArea, label.DesiredSize);

                // Arrange editor

                if (editor != null)
                {
                    var editorArea = new Rect(labelAreaWidth, y, editorAreaWidth, rowHeight);
                    ArrangeWithAlignment(editor, editorArea, editor.DesiredSize);
                }

                y += Math.Max(labelDesiredSize.Height, editorDesiredSize.Height);
            }

            return finalSize;
        }
    }
}
