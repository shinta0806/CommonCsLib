// ============================================================================
// 
// 複製時にイベント購読を複製しない ObservableObject
// Copyright (C) 2025 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      更新日      |                    更新内容
// ----------------------------------------------------------------------------
//  -.--  | 2025/02/07 (Fri) | 作成開始。
//  1.00  | 2025/02/07 (Fri) | ファーストバージョン。
// (1.01) | 2025/04/04 (Fri) |   partial 化。
// ============================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Shinta;

internal partial class CloneableObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
{
	// ====================================================================
	// public 関数
	// ====================================================================

	/// <summary>
	/// 深いコピー（購読を除く）
	/// </summary>
	/// <returns></returns>
	public CloneableObservableObject DeepClone()
	{
		// 複製
		CloneableObservableObject clone = (CloneableObservableObject)MemberwiseClone();
		DeepCloneCore(clone);

		// 購読も複製されているので解除する
		Delegate.InvocationListEnumerator<PropertyChangedEventHandler> propertyChangedHandlers = Delegate.EnumerateInvocationList(PropertyChanged);
		foreach (PropertyChangedEventHandler propertyChangedHandler in propertyChangedHandlers)
		{
			clone.PropertyChanged -= propertyChangedHandler;
		}
		Delegate.InvocationListEnumerator<PropertyChangingEventHandler> PropertyChangingHandlers = Delegate.EnumerateInvocationList(PropertyChanging);
		foreach (PropertyChangingEventHandler propertyChangingHandler in PropertyChangingHandlers)
		{
			clone.PropertyChanging -= propertyChangingHandler;
		}

		return clone;
	}

	// ====================================================================
	// protected 関数
	// ====================================================================

	/// <summary>
	/// コピー先に深いコピーをする（オーバーライド時は基底クラスのメソッドを呼び出すこと）
	/// </summary>
	/// <param name="dest"></param>
	protected virtual void DeepCloneCore(Object dest)
	{
	}

	// ====================================================================
	// ObservableObject のコード
	// ====================================================================

	/// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <inheritdoc cref="INotifyPropertyChanging.PropertyChanging"/>
	public event PropertyChangingEventHandler? PropertyChanging;

	/// <summary>
	/// Raises the <see cref="PropertyChanged"/> event.
	/// </summary>
	/// <param name="e">The input <see cref="PropertyChangedEventArgs"/> instance.</param>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="e"/> is <see langword="null"/>.</exception>
	protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(e);

		PropertyChanged?.Invoke(this, e);
	}

	/// <summary>
	/// Raises the <see cref="PropertyChanging"/> event.
	/// </summary>
	/// <param name="e">The input <see cref="PropertyChangingEventArgs"/> instance.</param>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="e"/> is <see langword="null"/>.</exception>
	protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(e);

#if false
		// When support is disabled, just do nothing
		if (!FeatureSwitches.EnableINotifyPropertyChangingSupport)
		{
			return;
		}
#endif

		PropertyChanging?.Invoke(this, e);
	}

	/// <summary>
	/// Raises the <see cref="PropertyChanged"/> event.
	/// </summary>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// Raises the <see cref="PropertyChanging"/> event.
	/// </summary>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	protected void OnPropertyChanging([CallerMemberName] string? propertyName = null)
	{
#if false
		// When support is disabled, avoid instantiating the event args entirely
		if (!FeatureSwitches.EnableINotifyPropertyChangingSupport)
		{
			return;
		}
#endif

		OnPropertyChanging(new PropertyChangingEventArgs(propertyName));
	}

	/// <summary>
	/// Compares the current and new values for a given property. If the value has changed,
	/// raises the <see cref="PropertyChanging"/> event, updates the property with the new
	/// value, then raises the <see cref="PropertyChanged"/> event.
	/// </summary>
	/// <typeparam name="T">The type of the property that changed.</typeparam>
	/// <param name="field">The field storing the property's value.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// The <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events are not raised
	/// if the current and new value for the target property are the same.
	/// </remarks>
	protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
	{
		// We duplicate the code here instead of calling the overload because we can't
		// guarantee that the invoked SetProperty<T> will be inlined, and we need the JIT
		// to be able to see the full EqualityComparer<T>.Default.Equals call, so that
		// it'll use the intrinsics version of it and just replace the whole invocation
		// with a direct comparison when possible (eg. for primitive numeric types).
		// This is the fastest SetProperty<T> overload so we particularly care about
		// the codegen quality here, and the code is small and simple enough so that
		// duplicating it still doesn't make the whole class harder to maintain.
		if (EqualityComparer<T>.Default.Equals(field, newValue))
		{
			return false;
		}

		OnPropertyChanging(propertyName);

		field = newValue;

		OnPropertyChanged(propertyName);

		return true;
	}

	/// <summary>
	/// Compares the current and new values for a given property. If the value has changed,
	/// raises the <see cref="PropertyChanging"/> event, updates the property with the new
	/// value, then raises the <see cref="PropertyChanged"/> event.
	/// See additional notes about this overload in <see cref="SetProperty{T}(ref T,T,string)"/>.
	/// </summary>
	/// <typeparam name="T">The type of the property that changed.</typeparam>
	/// <param name="field">The field storing the property's value.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> instance to use to compare the input values.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="comparer"/> is <see langword="null"/>.</exception>
	protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, IEqualityComparer<T> comparer, [CallerMemberName] string? propertyName = null)
	{
		ArgumentNullException.ThrowIfNull(comparer);

		if (comparer.Equals(field, newValue))
		{
			return false;
		}

		OnPropertyChanging(propertyName);

		field = newValue;

		OnPropertyChanged(propertyName);

		return true;
	}

	/// <summary>
	/// Compares the current and new values for a given property. If the value has changed,
	/// raises the <see cref="PropertyChanging"/> event, updates the property with the new
	/// value, then raises the <see cref="PropertyChanged"/> event.
	/// This overload is much less efficient than <see cref="SetProperty{T}(ref T,T,string)"/> and it
	/// should only be used when the former is not viable (eg. when the target property being
	/// updated does not directly expose a backing field that can be passed by reference).
	/// For performance reasons, it is recommended to use a stateful callback if possible through
	/// the <see cref="SetProperty{TModel,T}(T,T,TModel,Action{TModel,T},string?)"/> whenever possible
	/// instead of this overload, as that will allow the C# compiler to cache the input callback and
	/// reduce the memory allocations. More info on that overload are available in the related XML
	/// docs. This overload is here for completeness and in cases where that is not applicable.
	/// </summary>
	/// <typeparam name="T">The type of the property that changed.</typeparam>
	/// <param name="oldValue">The current property value.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="callback">A callback to invoke to update the property value.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// The <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events are not raised
	/// if the current and new value for the target property are the same.
	/// </remarks>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="callback"/> is <see langword="null"/>.</exception>
	protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback, [CallerMemberName] string? propertyName = null)
	{
		ArgumentNullException.ThrowIfNull(callback);

		// We avoid calling the overload again to ensure the comparison is inlined
		if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
		{
			return false;
		}

		OnPropertyChanging(propertyName);

		callback(newValue);

		OnPropertyChanged(propertyName);

		return true;
	}

	/// <summary>
	/// Compares the current and new values for a given property. If the value has changed,
	/// raises the <see cref="PropertyChanging"/> event, updates the property with the new
	/// value, then raises the <see cref="PropertyChanged"/> event.
	/// See additional notes about this overload in <see cref="SetProperty{T}(T,T,Action{T},string)"/>.
	/// </summary>
	/// <typeparam name="T">The type of the property that changed.</typeparam>
	/// <param name="oldValue">The current property value.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> instance to use to compare the input values.</param>
	/// <param name="callback">A callback to invoke to update the property value.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="comparer"/> or <paramref name="callback"/> are <see langword="null"/>.</exception>
	protected bool SetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, [CallerMemberName] string? propertyName = null)
	{
		ArgumentNullException.ThrowIfNull(comparer);
		ArgumentNullException.ThrowIfNull(callback);

		if (comparer.Equals(oldValue, newValue))
		{
			return false;
		}

		OnPropertyChanging(propertyName);

		callback(newValue);

		OnPropertyChanged(propertyName);

		return true;
	}

	/// <summary>
	/// Compares the current and new values for a given nested property. If the value has changed,
	/// raises the <see cref="PropertyChanging"/> event, updates the property and then raises the
	/// <see cref="PropertyChanged"/> event. The behavior mirrors that of <see cref="SetProperty{T}(ref T,T,string)"/>,
	/// with the difference being that this method is used to relay properties from a wrapped model in the
	/// current instance. This type is useful when creating wrapping, bindable objects that operate over
	/// models that lack support for notification (eg. for CRUD operations).
	/// Suppose we have this model (eg. for a database row in a table):
	/// <code>
	/// public class Person
	/// {
	///     public string Name { get; set; }
	/// }
	/// </code>
	/// We can then use a property to wrap instances of this type into our observable model (which supports
	/// notifications), injecting the notification to the properties of that model, like so:
	/// <code>
	/// public class BindablePerson : ObservableObject
	/// {
	///     public Model { get; }
	///
	///     public BindablePerson(Person model)
	///     {
	///         Model = model;
	///     }
	///
	///     public string Name
	///     {
	///         get => Model.Name;
	///         set => Set(Model.Name, value, Model, (model, name) => model.Name = name);
	///     }
	/// }
	/// </code>
	/// This way we can then use the wrapping object in our application, and all those "proxy" properties will
	/// also raise notifications when changed. Note that this method is not meant to be a replacement for
	/// <see cref="SetProperty{T}(ref T,T,string)"/>, and it should only be used when relaying properties to a model that
	/// doesn't support notifications, and only if you can't implement notifications to that model directly (eg. by having
	/// it inherit from <see cref="ObservableObject"/>). The syntax relies on passing the target model and a stateless callback
	/// to allow the C# compiler to cache the function, which results in much better performance and no memory usage.
	/// </summary>
	/// <typeparam name="TModel">The type of model whose property (or field) to set.</typeparam>
	/// <typeparam name="T">The type of property (or field) to set.</typeparam>
	/// <param name="oldValue">The current property value.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="model">The model containing the property being updated.</param>
	/// <param name="callback">The callback to invoke to set the target property value, if a change has occurred.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// The <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events are not
	/// raised if the current and new value for the target property are the same.
	/// </remarks>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="model"/> or <paramref name="callback"/> are <see langword="null"/>.</exception>
	protected bool SetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, [CallerMemberName] string? propertyName = null)
		where TModel : class
	{
		ArgumentNullException.ThrowIfNull(model);
		ArgumentNullException.ThrowIfNull(callback);

		if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
		{
			return false;
		}

		OnPropertyChanging(propertyName);

		callback(model, newValue);

		OnPropertyChanged(propertyName);

		return true;
	}

	/// <summary>
	/// Compares the current and new values for a given nested property. If the value has changed,
	/// raises the <see cref="PropertyChanging"/> event, updates the property and then raises the
	/// <see cref="PropertyChanged"/> event. The behavior mirrors that of <see cref="SetProperty{T}(ref T,T,string)"/>,
	/// with the difference being that this method is used to relay properties from a wrapped model in the
	/// current instance. See additional notes about this overload in <see cref="SetProperty{TModel,T}(T,T,TModel,Action{TModel,T},string)"/>.
	/// </summary>
	/// <typeparam name="TModel">The type of model whose property (or field) to set.</typeparam>
	/// <typeparam name="T">The type of property (or field) to set.</typeparam>
	/// <param name="oldValue">The current property value.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> instance to use to compare the input values.</param>
	/// <param name="model">The model containing the property being updated.</param>
	/// <param name="callback">The callback to invoke to set the target property value, if a change has occurred.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="comparer"/>, <paramref name="model"/> or <paramref name="callback"/> are <see langword="null"/>.</exception>
	protected bool SetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, [CallerMemberName] string? propertyName = null)
		where TModel : class
	{
		ArgumentNullException.ThrowIfNull(comparer);
		ArgumentNullException.ThrowIfNull(model);
		ArgumentNullException.ThrowIfNull(callback);

		if (comparer.Equals(oldValue, newValue))
		{
			return false;
		}

		OnPropertyChanging(propertyName);

		callback(model, newValue);

		OnPropertyChanged(propertyName);

		return true;
	}

	/// <summary>
	/// Compares the current and new values for a given field (which should be the backing
	/// field for a property). If the value has changed, raises the <see cref="PropertyChanging"/>
	/// event, updates the field and then raises the <see cref="PropertyChanged"/> event.
	/// The behavior mirrors that of <see cref="SetProperty{T}(ref T,T,string)"/>, with the difference being that
	/// this method will also monitor the new value of the property (a generic <see cref="Task"/>) and will also
	/// raise the <see cref="PropertyChanged"/> again for the target property when it completes.
	/// This can be used to update bindings observing that <see cref="Task"/> or any of its properties.
	/// This method and its overload specifically rely on the <see cref="TaskNotifier"/> type, which needs
	/// to be used in the backing field for the target <see cref="Task"/> property. The field doesn't need to be
	/// initialized, as this method will take care of doing that automatically. The <see cref="TaskNotifier"/>
	/// type also includes an implicit operator, so it can be assigned to any <see cref="Task"/> instance directly.
	/// Here is a sample property declaration using this method:
	/// <code>
	/// private TaskNotifier myTask;
	///
	/// public Task MyTask
	/// {
	///     get => myTask;
	///     private set => SetAndNotifyOnCompletion(ref myTask, value);
	/// }
	/// </code>
	/// </summary>
	/// <param name="taskNotifier">The field notifier to modify.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// The <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events are not raised if the current
	/// and new value for the target property are the same. The return value being <see langword="true"/> only
	/// indicates that the new value being assigned to <paramref name="taskNotifier"/> is different than the previous one,
	/// and it does not mean the new <see cref="Task"/> instance passed as argument is in any particular state.
	/// </remarks>
	protected bool SetPropertyAndNotifyOnCompletion([NotNull] ref TaskNotifier? taskNotifier, Task? newValue, [CallerMemberName] string? propertyName = null)
	{
		// We invoke the overload with a callback here to avoid code duplication, and simply pass an empty callback.
		// The lambda expression here is transformed by the C# compiler into an empty closure class with a
		// static singleton field containing a closure instance, and another caching the instantiated Action<TTask>
		// instance. This will result in no further allocations after the first time this method is called for a given
		// generic type. We only pay the cost of the virtual call to the delegate, but this is not performance critical
		// code and that overhead would still be much lower than the rest of the method anyway, so that's fine.
		return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier(), newValue, null, propertyName);
	}

	/// <summary>
	/// Compares the current and new values for a given field (which should be the backing
	/// field for a property). If the value has changed, raises the <see cref="PropertyChanging"/>
	/// event, updates the field and then raises the <see cref="PropertyChanged"/> event.
	/// This method is just like <see cref="SetPropertyAndNotifyOnCompletion(ref TaskNotifier,Task,string)"/>,
	/// with the difference being an extra <see cref="Action{T}"/> parameter with a callback being invoked
	/// either immediately, if the new task has already completed or is <see langword="null"/>, or upon completion.
	/// </summary>
	/// <param name="taskNotifier">The field notifier to modify.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="callback">A callback to invoke to update the property value.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// The <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events are not raised
	/// if the current and new value for the target property are the same.
	/// </remarks>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="callback"/> is <see langword="null"/>.</exception>
	protected bool SetPropertyAndNotifyOnCompletion([NotNull] ref TaskNotifier? taskNotifier, Task? newValue, Action<Task?> callback, [CallerMemberName] string? propertyName = null)
	{
		ArgumentNullException.ThrowIfNull(callback);

		return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier(), newValue, callback, propertyName);
	}

	/// <summary>
	/// Compares the current and new values for a given field (which should be the backing
	/// field for a property). If the value has changed, raises the <see cref="PropertyChanging"/>
	/// event, updates the field and then raises the <see cref="PropertyChanged"/> event.
	/// The behavior mirrors that of <see cref="SetProperty{T}(ref T,T,string)"/>, with the difference being that
	/// this method will also monitor the new value of the property (a generic <see cref="Task"/>) and will also
	/// raise the <see cref="PropertyChanged"/> again for the target property when it completes.
	/// This can be used to update bindings observing that <see cref="Task"/> or any of its properties.
	/// This method and its overload specifically rely on the <see cref="TaskNotifier{T}"/> type, which needs
	/// to be used in the backing field for the target <see cref="Task"/> property. The field doesn't need to be
	/// initialized, as this method will take care of doing that automatically. The <see cref="TaskNotifier{T}"/>
	/// type also includes an implicit operator, so it can be assigned to any <see cref="Task"/> instance directly.
	/// Here is a sample property declaration using this method:
	/// <code>
	/// private TaskNotifier&lt;int&gt; myTask;
	///
	/// public Task&lt;int&gt; MyTask
	/// {
	///     get => myTask;
	///     private set => SetAndNotifyOnCompletion(ref myTask, value);
	/// }
	/// </code>
	/// </summary>
	/// <typeparam name="T">The type of result for the <see cref="Task{TResult}"/> to set and monitor.</typeparam>
	/// <param name="taskNotifier">The field notifier to modify.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// The <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events are not raised if the current
	/// and new value for the target property are the same. The return value being <see langword="true"/> only
	/// indicates that the new value being assigned to <paramref name="taskNotifier"/> is different than the previous one,
	/// and it does not mean the new <see cref="Task{TResult}"/> instance passed as argument is in any particular state.
	/// </remarks>
	protected bool SetPropertyAndNotifyOnCompletion<T>([NotNull] ref TaskNotifier<T>? taskNotifier, Task<T>? newValue, [CallerMemberName] string? propertyName = null)
	{
		return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier<T>(), newValue, null, propertyName);
	}

	/// <summary>
	/// Compares the current and new values for a given field (which should be the backing
	/// field for a property). If the value has changed, raises the <see cref="PropertyChanging"/>
	/// event, updates the field and then raises the <see cref="PropertyChanged"/> event.
	/// This method is just like <see cref="SetPropertyAndNotifyOnCompletion{T}(ref TaskNotifier{T},Task{T},string)"/>,
	/// with the difference being an extra <see cref="Action{T}"/> parameter with a callback being invoked
	/// either immediately, if the new task has already completed or is <see langword="null"/>, or upon completion.
	/// </summary>
	/// <typeparam name="T">The type of result for the <see cref="Task{TResult}"/> to set and monitor.</typeparam>
	/// <param name="taskNotifier">The field notifier to modify.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="callback">A callback to invoke to update the property value.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// The <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events are not raised
	/// if the current and new value for the target property are the same.
	/// </remarks>
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="callback"/> is <see langword="null"/>.</exception>
	protected bool SetPropertyAndNotifyOnCompletion<T>([NotNull] ref TaskNotifier<T>? taskNotifier, Task<T>? newValue, Action<Task<T>?> callback, [CallerMemberName] string? propertyName = null)
	{
		ArgumentNullException.ThrowIfNull(callback);

		return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier<T>(), newValue, callback, propertyName);
	}

	/// <summary>
	/// Implements the notification logic for the related methods.
	/// </summary>
	/// <typeparam name="TTask">The type of <see cref="Task"/> to set and monitor.</typeparam>
	/// <param name="taskNotifier">The field notifier.</param>
	/// <param name="newValue">The property's value after the change occurred.</param>
	/// <param name="callback">(optional) A callback to invoke to update the property value.</param>
	/// <param name="propertyName">(optional) The name of the property that changed.</param>
	/// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
	private bool SetPropertyAndNotifyOnCompletion<TTask>(ITaskNotifier<TTask> taskNotifier, TTask? newValue, Action<TTask?>? callback, [CallerMemberName] string? propertyName = null)
		where TTask : Task
	{
		if (ReferenceEquals(taskNotifier.Task, newValue))
		{
			return false;
		}

		// Check the status of the new task before assigning it to the
		// target field. This is so that in case the task is either
		// null or already completed, we can avoid the overhead of
		// scheduling the method to monitor its completion.
		bool isAlreadyCompletedOrNull = newValue?.IsCompleted ?? true;

		OnPropertyChanging(propertyName);

		taskNotifier.Task = newValue;

		OnPropertyChanged(propertyName);

		// If the input task is either null or already completed, we don't need to
		// execute the additional logic to monitor its completion, so we can just bypass
		// the rest of the method and return that the field changed here. The return value
		// does not indicate that the task itself has completed, but just that the property
		// value itself has changed (ie. the referenced task instance has changed).
		// This mirrors the return value of all the other synchronous Set methods as well.
		if (isAlreadyCompletedOrNull)
		{
			if (callback is not null)
			{
				callback(newValue);
			}

			return true;
		}

		// We use a local async function here so that the main method can
		// remain synchronous and return a value that can be immediately
		// used by the caller. This mirrors Set<T>(ref T, T, string).
		// We use an async void function instead of a Task-returning function
		// so that if a binding update caused by the property change notification
		// causes a crash, it is immediately reported in the application instead of
		// the exception being ignored (as the returned task wouldn't be awaited),
		// which would result in a confusing behavior for users.
		async void MonitorTask()
		{
			// Await the task and ignore any exceptions
			await newValue!.GetAwaitableWithoutEndValidation();

			// Only notify if the property hasn't changed
			if (ReferenceEquals(taskNotifier.Task, newValue))
			{
				OnPropertyChanged(propertyName);
			}

			if (callback is not null)
			{
				callback(newValue);
			}
		}

		MonitorTask();

		return true;
	}

	/// <summary>
	/// An interface for task notifiers of a specified type.
	/// </summary>
	/// <typeparam name="TTask">The type of value to store.</typeparam>
	private interface ITaskNotifier<TTask>
		where TTask : Task
	{
		/// <summary>
		/// Gets or sets the wrapped <typeparamref name="TTask"/> value.
		/// </summary>
		TTask? Task { get; set; }
	}

	/// <summary>
	/// A wrapping class that can hold a <see cref="Task"/> value.
	/// </summary>
	protected sealed class TaskNotifier : ITaskNotifier<Task>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TaskNotifier"/> class.
		/// </summary>
		internal TaskNotifier()
		{
		}

		private Task? task;

		/// <inheritdoc/>
		Task? ITaskNotifier<Task>.Task
		{
			get => this.task;
			set => this.task = value;
		}

		/// <summary>
		/// Unwraps the <see cref="Task"/> value stored in the current instance.
		/// </summary>
		/// <param name="notifier">The input <see cref="TaskNotifier{TTask}"/> instance.</param>
		public static implicit operator Task?(TaskNotifier? notifier)
		{
			return notifier?.task;
		}
	}

	/// <summary>
	/// A wrapping class that can hold a <see cref="Task{T}"/> value.
	/// </summary>
	/// <typeparam name="T">The type of value for the wrapped <see cref="Task{T}"/> instance.</typeparam>
	protected sealed class TaskNotifier<T> : ITaskNotifier<Task<T>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TaskNotifier{TTask}"/> class.
		/// </summary>
		internal TaskNotifier()
		{
		}

		private Task<T>? task;

		/// <inheritdoc/>
		Task<T>? ITaskNotifier<Task<T>>.Task
		{
			get => this.task;
			set => this.task = value;
		}

		/// <summary>
		/// Unwraps the <see cref="Task{T}"/> value stored in the current instance.
		/// </summary>
		/// <param name="notifier">The input <see cref="TaskNotifier{TTask}"/> instance.</param>
		public static implicit operator Task<T>?(TaskNotifier<T>? notifier)
		{
			return notifier?.task;
		}
	}
}