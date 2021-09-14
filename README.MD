# Pickle
[![openupm](https://img.shields.io/npm/v/com.mpozek.pickle?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.mpozek.pickle/)

A better Object picking solution for Unity.

![Pickle_Preview](https://user-images.githubusercontent.com/15526815/132820255-99ac69b3-c26c-4b11-9a70-a20b0e1ce266.gif)

## What is Pickle?
Pickle is a collection of Editor scripts and Attributes that aim to replicate and extend the behaviour of the default UnityEngine.Object picker.

## Installation
Using OpenUPM CLI:
`openupm add com.mpozek.pickle`

Or manually:
1. open the Unity Package Manager (Windows/Package Manager)
2. click the add package button and choose the "add package from git URL" option

![image](https://user-images.githubusercontent.com/15526815/130480877-e7b244be-7a24-4bf7-b008-ca214f090ba5.png)

3. paste the git url `https://github.com/MPozek/Pickle.git` and click Add

That's it! Unity should handle the rest of downloading and adding Pickle to your project.

## Why use Pickle?
- it correctly displays scene Components and prefabs in the picker if they match the field type
- it allows you to choose if the object picker will display only assets, scene objects or both
- it offers a way to define further filtering methods per attribute
- it can open the Object selector in a window, or a dropdown, depending on which you find neater
- it works on both fields and arrays/lists of types derived from `UnityEngine.Object`
- it looks exactly like the built-in unity object picker so you can keep your editor UI feeling clean and natural
- it's easy, just add the `[Pickle]` attribute above your field

## Usage example
```cs
// it's easy
[Pickle]
public Rigidbody DefaultPicker;

// open the picker as a dropdown, this is the default behaviour
[Pickle(PickerType = PickerType.Dropdown)]
public GameObject DropdownPicker;

// open the picker in a new window
[Pickle(PickerType = PickerType.Window)]
public GameObject CustomWindowPicker;

// supports both arrays and lists
[Pickle]
public Rigidbody[] RigidbodyArray;
[Pickle]
public List<Rigidbody> RigidbodyList;

// narrow down the allowed objects on the field, the default is both assets and scene
[Pickle(LookupType = ObjectProviderType.Scene)]
public Transform OnlySceneObjects;

[Pickle(LookupType = ObjectProviderType.Assets)]
public Transform OnlyPrefabs;

[Pickle(LookupType = ObjectProviderType.Children)]
public Transform OnlyChildrenOfThisObject;

[Pickle(LookupType = ObjectProviderType.RootChildren)]
public Transform OnlyChildrenOfTheRootOfThisObject;

[Pickle(LookupType = ObjectProviderType.Assets | ObjectProviderType.Scene)]
public Transform AnyAssetOrSceneObject;

// use a custom method to filter your results
[Pickle(LookupType = ObjectProviderType.Scene, FilterMethodName = nameof(CustomFilter))]
public UnityEngine.UI.Image ImagesWithCustomFilter;

private bool CustomFilter(ObjectTypePair item)
{
    return item.Object.name.StartsWith("UI_");
}

[Header("Auto picking")]
[Pickle(AutoPickMode = AutoPickMode.GetComponent)]
public Transform MyTransform;

[Pickle(AutoPickMode = AutoPickMode.GetComponentInChildren)]
public MeshRenderer ChildRenderer;

[Pickle(AutoPickMode = AutoPickMode.GetComponentInParent)]
public Rigidbody ParentRigidbody;

[Pickle(AutoPickMode = AutoPickMode.FindObject)]
public Camera SceneCamera;
```
