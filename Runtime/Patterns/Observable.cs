using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reference type that lets you declare observable properties. It is a CLASS (not a struct): a mutable
/// struct with delegate fields silently loses its subscribers whenever it is copied (returned by value
/// from a property, passed by value, captured), so subscriptions applied to a copy do nothing. As a class
/// it can be exposed via a get-only property safely. Declare and initialize the field, e.g.:
///
/// public Observable<float> Speed = new Observable<float>();
///
/// then, in any other class, you can register to OnValueChanged events on that property (usually in OnEnable) :
///
/// protected virtual void OnEnable()
/// {
///     _myCharacter.Speed.OnValueChanged += OnSpeedChange;
/// }
///
/// and unsubscribe like so :
///
/// protected virtual void OnDisable()
/// {
///     _myCharacter.Speed.OnValueChanged -= OnSpeedChange;
/// }
///
/// and then all you need is a method to handle that speed change :
///
/// protected virtual void OnSpeedChange()
/// {
///     Debug.Log(_myCharacter.Speed.Value);
/// }
///
/// </summary>
/// <typeparam name="T"></typeparam>
[Serializable]
public class Observable<T>
{
    public Action OnValueChanged;
    public Action<T> OnValueChangedTo;
    public Action<T, T> OnValueChangedFromTo;

    private T _value;

    public Observable() { }

    public Observable(T initialValue)
    {
        _value = initialValue;
    }

    public T Value
    {
        get { return _value; }
        set
        {
            if (!EqualityComparer<T>.Default.Equals(value, _value))
            {
                var prev = _value;
                _value = value;
                OnValueChanged?.Invoke();
                OnValueChangedTo?.Invoke(_value);
                OnValueChangedFromTo?.Invoke(prev, _value);
            }
        }
    }
}