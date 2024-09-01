using Microsoft.Xna.Framework;

using System.Windows.Media;
using System.Windows;

using System.Windows;
using System.Windows.Media;

namespace Courage.AnimTool;

public static class WpfToMonoGameExtensions
{
    public static Vector2 ToVector2(this System.Windows.Point point) => new Vector2((float)point.X, (float)point.Y);


public static T FindChild<T>(this DependencyObject parent) where T : DependencyObject
{
	// Check if the parent is null
	if(parent == null) return null;

	// Iterate through the children of the parent
	for(int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
	{
		var child = VisualTreeHelper.GetChild(parent, i);

		// Check if the child is of the requested type
		if(child is T)
		{
			return (T)child;
		}

		// Recursively search through the child's children
		var result = FindChild<T>(child);
		if(result != null)
		{
			return result;
		}
	}

	return null;
}
}