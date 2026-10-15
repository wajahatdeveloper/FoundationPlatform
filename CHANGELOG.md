# Changelog

All notable changes to this package are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/); versioning follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Extensions trimmed to what the code uses

- **Removed** 1,381 members of the `Runtime/Extensions` static classes with no caller in this repository (call sites found by a Roslyn token scan over every package, `Samples~` and `Assets/Scripts`, under editor, all-symbols and no-symbols preprocessor sets; members kept code depends on were kept transitively). The folder went from 21.6k to 6.6k lines. `Deconstruct` on `Vector2`/`Vector3` is kept (called implicitly by tuple deconstruction), and the `RandomX` `UnityEngine.Random` facade (`RandomX.Unity.cs`) is kept whole because its contract is member-for-member parity.
- **Removed** whole files: `PlayerPrefsX` (use `PersistentDataHandler`), `TimeX`, the LINQ-to-GameObject traversal enumerables in `GameObjectExtensions` (`Ancestors`/`Descendants`/`BeforeSelf`/`AfterSelf`), and the Animation-clip, Fade, Bitwise, Array, IComparable, IEquatable, Type, Collider, NavMesh, Graphic, RenderTexture, TimeSpan, Base64, File, OR, Pipes, Targeting, Validation and EventSystem extension files.
- **Removed** `MonoBehaviourExtensions.RunOnce`, `CollectionExtensions.GetRandom`, `MathExtensions.GetDirectionFromSpread` and `RandomX.NextWeightedInd` among the rest. Full list by class:

<details><summary>Removed members</summary>

- `AnimationClipExtensions`: AddAnimationEvent
- `AnimatorExtensions`: GetCrossFadeProgress, HasParameter, IsInState, WaitForCurrentStateToFinish, WaitForStateToFinish
- `ArrayExtensions`: HasIndex
- `AudioSourceExtensions`: FadeToVolume, Mute, PlayRandom, ToggleMute, Unmute
- `Base64ImageExtensions`: ToBase64String, ToTexture2DFromBase64String
- `BitwiseExtensions`: GetBit, RefSetBit, SetBit
- `CameraExtensions`: ChangeFieldOfView, ChangeOrthographicSize, GetRightDirection, GetUpDirection, GetViewportBounds, IsInViewport, IsWorldPointInViewport, MoveAndLookAt, RaycastFromScreen, RaycastFromScreenCenter, RotateTo, ScreenCenterToRay, ScreenToWorldPointOnGround, ScreenToWorldPointOnPlane, SetAspectRatio, SetFarClipPlane, SetFieldOfView, SetNearClipPlane, SetOrthographicSize, WorldPointOffsetByDepth
- `CanvasExtensions`: GetScaleFactor
- `CollectionExtensions`: ContentsMatch, ContentsMatchKeys, ContentsMatchValues, DisposeIfNotNull, FillBy, GetRandom, GetRandomInRange, IndexOfItem, InsertAt, NextIndexInCircle, OneHitPerInstance, OneHitPerInstanceList, OnePerInstance, SwapInPlace
- `ColliderExtensions`: GetRandomPointInsideCollider, IsColliderInside
- `Color32Extensions`: WithB, WithBA, WithG, WithGA, WithGB, WithGBA, WithR, WithRA, WithRB, WithRBA, WithRG, WithRGA, WithRGB
- `ColorExtensions`: Brightness, Closest, Decode, GetColorFromRGB255, GetDiff, GetHSV, HexToColor, MakeRandomColor, SetA, ToHex, ToHsl, WithB, WithBA, WithG, WithGA, WithGB, WithGBA, WithR, WithRA, WithRB, WithRBA, WithRG, WithRGA, WithRGB
- `ColorX`: AliceBlue, AntiqueWhite, Aquamarine, Azure, Beige, BestRed, Bisque, BlanchedAlmond, BlueViolet, Brown, Burlywood, CadetBlue, Chartreuse, Chocolate, ColorDictionary, ColoringMode, Colorize, Coral, CornflowerBlue, Cornsilk, Crimson, Cyan, DarkBlue, DarkCyan, DarkGoldenrod, DarkGray, DarkGreen, DarkKhaki, DarkMagenta, DarkOliveGreen, DarkOrange, DarkOrchid, DarkRed, DarkSalmon, DarkSeaGreen, DarkSlateBlue, DarkSlateGray, DarkTurquoise, DarkViolet, DeepPink, DeepSkyBlue, DimGray, DodgerBlue, FireBrick, FloralWhite, ForestGreen, Fuchsia, Gainsboro, GetColorAt, GhostWhite, Gold, Goldenrod, Gray, GreenYellow, Honeydew, HotPink, IndianRed, Indigo, InitializeDictionary, Ivory, Khaki, Lavender, Lavenderblush, LawnGreen, LemonChiffon, LightBlue, LightCoral, LightCyan, LightGoldenodYellow, LightGray, LightGreen, LightPink, LightSalmon, LightSeaGreen, LightSkyBlue, LightSlateGray, LightSteelBlue, LightYellow, Lime, LimeGreen, Linen, Magenta, Maroon, MediumAquamarine, MediumBlue, MediumOrchid, MediumPurple, MediumSeaGreen, MediumSlateBlue, MediumSpringGreen, MediumTurquoise, MediumVioletRed, MidnightBlue, Mintcream, MistyRose, Moccasin, NavajoWhite, Navy, OldLace, Olive, Olivedrab, Orangered, Orchid, PaleGoldenrod, PaleGreen, PaleTurquoise, PaleVioletred, PapayaWhip, PeachPuff, Peru, Pink, Plum, PowderBlue, RandomColor, ReunoYellow, RosyBrown, RoyalBlue, SaddleBrown, Salmon, SandyBrown, SeaGreen, Seashell, Sienna, Silver, SkyBlue, SlateBlue, SlateGray, Snow, SpringGreen, SteelBlue, Teal, Thistle, Tomato, Turquoise, Violet, Wheat, WhiteSmoke, YellowGreen
- `EnumExtensions`: ToByte
- `EventSystemExtensions`: GetEventSystemRaycastResults, IsPointerOverUIElement, UiLayer
- `FadeExtensions`: FadeCanvasGroup, FadeImage, FadeSprite, FadeText
- `FileExtensions`: CreateDirectoryIfNotExists
- `GameObjectCollectionExtensions`: Ancestors, AncestorsAndSelf, ChildrenAndSelf, Descendants, DescendantsAndSelf
- `GameObjectComponentExtensions`: ComponentOfInterface, DoMessage, FindObjectsOfInterface, FindObjectsOfInterfaceAsComponents, ForEachChildOfType, ForEachRootChildOfType, GetClasses, GetClassesInChildren, GetClassInChildren, GetComponentOnObject, GetComponentOnObjectOrParent, GetComponentsInChildrenWithTag, GetIComponent, GetInterface, GetInterfaceInChildren, GetInterfacesInChildren, GetOrAddComponent, HasComponent
- `GameObjectExtensions`: AfterSelfEnumerable, AncestorsEnumerable, BeforeSelfEnumerable, DescendantsEnumerable, EnableComponentIfExists, GetPrefabName, RemoveComponentIfExists, RemoveComponentsIfExists
- `GameObjectHierarchyExtensions`: AfterSelf, AfterSelfAndSelf, Ancestors, AncestorsAndSelf, BeforeSelf, BeforeSelfAndSelf, CheckChildsOfLayer, Child, ChildrenAndSelf, Descendants, DescendantsAndSelf, GetChildByName, GetChildrenComponent, GetChildrenWithName, GetObjectsOfLayerInChilds
- `GameObjectUtilityExtensions`: ActivateAndParent, DestroySelf, DistanceTo, FindNearestByTag, FindNearestByType, FindNearests, GetCollisionMask, HasAnimation, HasCharacterController, HasRigidbody, IsInLayerMask, IsNullOrInactive, ReverseActiveState, SetActiveTSCH, SetActiveWithAncestors, SetActiveWithChildren, SetActiveWithDescendants, SetActiveWithParent, SetCollisionRecursively, SetLossyScale, SetTransformX, SetTransformY, SetTransformZ
- `GraphicExtensions`: SetColorA, SetColorB, SetColorBA, SetColorG, SetColorGA, SetColorGB, SetColorGBA, SetColorR, SetColorRA, SetColorRB, SetColorRBA, SetColorRG, SetColorRGA, SetColorRGB
- `ICollectionExtensions`: AddIf, AddIfNotContains, AddRangeIf, AddRangeIfNotContains, ContainsAll, ContainsAny, RemoveIf, RemoveIfContains, RemoveRangeIf, RemoveRangeIfContains, RemoveWhere
- `IComparableExtensions`: IsBetween
- `IDictionaryExtensions`: AddIfNotContainsKey, ContainsAllKey, ContainsAnyKey, KeyByValue, RemoveIfContainsKey, RemoveNullValues, ToHashtable, ToNameValueCollection, ToSortedDictionary, TryGetTypedValue
- `IEnumerableExtensions`: AsList, AsString, CompareChanges, ConcatString, ExceptBy, FirstOrNull, Flatten, FlattenIterator, GetRandomElements, GetRandomElementWithProbability, IntersectBy, Intersperse, IntersperseIterator, IsNotNullOrEmpty, MaxBy, MaxElement, MaxId, MinBy, OrEmpty, Rewrite, TakeUntil, ToObservableCollection, ToSortedSet, UnityConvertAll, WhereIf, Zip
- `IEquatableExtensions`: EqualsToAll, EqualsToAny
- `IListExtensions`: AddRangeUntyped, AtWrapped, AtWrappedOrDefault, ChangeIndex, ChangeIndexInternal, ClearAndDispose, GetRandom, HasIndex, IndexOfOrDefault, PopLast, PopRandom, PopRandomElementWithProbability, PopRandoms, RandomOrDefault, RemoveDefaultValues, RemoveFirst, RemoveNullEntries, RemoveRangeUntyped, ReplaceUntyped, SetAtWrapped, SortedInsert, TryFindIndex, WrapIndex
- `LayerMaskExtension`: AddToMask, AsString, ContainsLayer, GetMask, IsInLayerMask, IsLayerInLayerMask, MaskIncludes, MaskToNames, MaskToString, RemoveFromMask
- `MathExtensions`: abbreviations, Approx, ApproxZero, Complement, ConvertBoolArrayToByte, ConvertByteToBoolArray, DegreeToRadian, ExtractPosition, ExtractRotation, ExtractScale, GetDirectionFromSpread, GetPercent, IntToLayerMask, LimitDecimals, LineIntersection3D, LineIntersectsLine, LineIntersectsRect, MirrorBy, RadianToDegree, RandomNegPos, SetGlobalScale, ShortestRotationTo, Sqr, Squared, ToStringAbbreviated, TwoDimenionalLookAt, WrapValue, XNOR
- `MathX`: AngleBetweenIsSmaller, AngleDirection, Approx, Berp, Bounce, Chance, ClampAngle, ClampedRemap, Clerp, ClosestPointIsA, ConvertSeconds, Coserp, Detection2D, Detection3D, Detection3DAll, DirectionFromAngle, DirectionFromAngle2D, DistanceBetweenPointAndLine, DotValueForNormalizedAngles, GaussFalloff, GOLDENRATIO, GRAVITATIONALCONSTANT, Hermite, InRange, InRangeInclusive, InverseLerpUnclamped, LIGHTYEAR, map01, map10, PLANCKLENGTH, PLANCKMASS, PLANCKSCONSTANT, PLANCKTEMPERATURE, PLANCKTIME, PLASTICNUMBER, ProjectPointOnLine, Remap, ReMapClamped, RollADice, RoundToClosest, RoundToDecimal, Sinerp, Slerp2D, SlerpWithAxises, SmoothStepUnclamped, SPEEDOFLIGHT, SuperLerp, SuperLerpUnclamped, TAU
- `MonoBehaviourExtensions`: GetActionKey, GetPersistentActionKey, RanOnceKeys, RunOnce, RunOncePersistent
- `NavMeshExtensions`: GetLength, GetPointOnPath, GetPointsOnPath, GetTimeToPass
- `NullChecks`: IsNotNull, VerifyNotNull
- `ORExtension`: IsOneOf
- `Physics2DExtensions`: GetClosestCollider2D, GetCollidersInRange2D, GetGroundHeight2D, GetGroundNormal2D, IsOnGround2D, IsPositionClear2D, OverlapBox2D, OverlapCapsule2D, OverlapCircle2D, Raycast2DDown, Raycast2DTo, Raycast2DUp
- `PhysicsExtensions`: FindNearestClearPosition, GetClosestCollider, GetCollidersInRange, GetGroundHeight, GetGroundNormal, IsOnGround, RaycastDown, RaycastTo, RaycastUp
- `Pipes`: Pipe, PipeKeep
- `QuaternionExtensions`: AddEulerX, AddEulerY, AddEulerZ, AngleTo, ClampEuler, LerpTo, LookAt2D, LookAtY, SetEulerX, SetEulerY, SetEulerZ, SlerpTo, SlerpUnclampedTo, SmoothRotateTowards, SmoothRotateTowardsDamped, SmoothRotateTowardsLerp
- `RandomX`: NextBool, NextDiscrete, NextEnum, NextWeightedInd
- `RectExtensions`: AddHeight, AddSize, AddWidth, AlignHorizontallyCenteredTo, AlignToBottomLeftOf, AlignToBottomOf, AlignToBottomRightOf, AlignToLeftOf, AlignToRightOf, AlignToTopLeftOf, AlignToTopOf, AlignToTopRightOf, AlignVerticallyCenteredTo, BottomHalf, CenterTo, Inflate, Intersects, LeftHalf, LeftOf, Restrict, RightHalf, RightOf, SplitHorizontal, SplitHorizontalAndCombine, SplitVertical, SplitVerticalAndCombine, TopHalf, TranslateX, TranslateY, WithCenter, WithHeight, WithMax, WithMin, WithPosition, WithSize, WithWidth, WithXMax, WithXMin, WithYMax, WithYMin
- `RectTransformExtensions`: _anchorPresets, AnchorOrigin, AnchorOriginParent, AnchorToParentSpace, ChildToParentSpace, GetAnchors, GetBottom, GetHeight, GetLeft, GetRectTransform, GetRight, GetRootCanvas, GetScreenRect, GetSize, GetTop, GetWidth, GetWorldRect, MoveBottomInside, MoveBottomOutside, MoveFrom, MoveInside, MoveLeftInside, MoveLeftOutside, MoveOutside, MoveRight, MoveRightInside, MoveRightOutside, MoveTop, MoveTopInside, MoveTopOutside, OffsetPositionX, OffsetPositionY, ParentToAnchorSpace, ParentToChildSpace, RectSetting, RectTransformToScreenSpace, SetAnchor, SetAnchoredPosition3DX, SetAnchoredPosition3DXY, SetAnchoredPosition3DXZ, SetAnchoredPosition3DY, SetAnchoredPosition3DYZ, SetAnchoredPosition3DZ, SetAnchoredPositionX, SetAnchoredPositionY, SetAnchorMaxX, SetAnchorMaxY, SetAnchorMinX, SetAnchorMinY, SetAnchors, SetBottomFrom, SetDefaultScale, SetHeight, SetLeftBottomPosition, SetLeftFrom, SetLeftTopPosition, SetPivotAndAnchors, SetPivotOnly, SetPivotOnlyX, SetPivotOnlyY, SetPivotX, SetPivotY, SetPositionOfPivot, SetPositionX, SetPositionY, SetRelativeBottom, SetRelativeLeft, SetRelativeRight, SetRelativeTop, SetRightBottomPosition, SetRightFrom, SetRightTopPosition, SetSize, SetSizeDeltaX, SetSizeDeltaY, SetTopFrom, SetWidth, switchToRectTransform, ToScreenSpace
- `ReflectionExtensions`: HasField, HasMethod
- `RegexPattern`: AlphanumericWithoutSpace, AlphanumericWithSpace, Email, EmailAddress, EmptyOrWhiteSpace, ExtractFromBrackets, ExtractFromCurlyBrackets, ExtractFromSquareBrackets, FloatingNumber, hexadecimalRegex, HexCode, htmlRegex, HTMLTag, IPAddress, URL, URL_STRICT, variableRegex, WholeNumber
- `RendererExtensions`: IsVisibleFrom
- `RenderTextureExtensions`: ToSprite, ToTexture2D, WriteToSprite, WriteToTexture2D
- `RichTextExtensions`: Italics
- `Rigidbody2DExtensions`: AddForceAwayFrom, AddForceTowards, AddForwardForce, AddUpForce, ClampVelocity, DisableGravity, EnableGravity, GetAngularSpeed, IsGroundedCircle, IsRotating, StopAllMovement, StopAngularVelocity, StopVelocity, ToggleGravity, ToggleKinematic, Unfreeze
- `RigidbodyExtensions`: AddForceAwayFrom, AddForceInDirection, AddForceTowards, AddForwardForce, AddRightForce, AddUpForce, ClampAngularVelocity, ClampVelocity, DisableGravity, EnableGravity, GetAngularSpeed, GetGroundNormal, IsGroundedSphere, IsRotating, SetBodyState, SetVelocityInDirection, SetVelocityTowards, StopAllMovement, StopAngularVelocity, StopVelocity, ToggleGravity, ToggleKinematic, Unfreeze
- `SceneManagerX`: LoadNextScene, LoadNextSceneAsync, LoadPreviousScene, LoadPreviousSceneAsync, LoadScene, RestartCurrentScene, RestartCurrentSceneAsync
- `StackQueueExtensions`: EnqueueRange, PushRange, Slice
- `StreamExts`: CopyToFile, CopyToFileAsync, GetFileMD5, GetFileSha1, HashFile, SaveFile
- `StringExtensions`: AllAfter, AsEnum, AsSpan, ContainsAnyOf, EndsWithAny, Fmt, FromLeft, FromRight, IsNumber, IsWhitespace, Italics, KeepMatching, ParseColor, RemoveSpecialCharacters, RemoveWhitespaces, Sized, StartsWithAny, SurroundedWith, ToCamelCase, ToEnum, ToRoman, Underlined, Without, WithoutPrefix, WithoutSuffix
- `TargetingExtensions`: FindNearestUnit, FindNearestUnit_List, FindNearestUnit_ListRange, FindNearestUnit_ListRangeView, FindNearestUnit_ListView, FindNearestUnit_Range, FindNearestUnit_RangeView, FindNearestUnit_View, FLOAT_MaxSearchRange
- `Texture2DExtensions`: AsSprite, ClearMipMapBorders, Resample, RotateTexture, ScaleTexture, ScaleTextureBilinear, SizeTextureBilinear, TextureScaleMode, ToTexture2D, WithSolidColor
- `TimeSpanExtensions`: ToHumanReadableString, ToSeconds, ToTimeSpan
- `TypeExtensions`: ImplementsInterface
- `types (PlayerPrefsX.cs)`: PlayerPrefsX
- `types (RectTransformExtensions.cs)`: MinMax01, RectAnchor
- `types (TimeX.cs)`: TimeX
- `UIExtensions`: AddClickListener, AddValueChangedListener, CenterInParent, ClearClickListeners, ClearText, FadeToAlpha, FindUIElement, GetAllUIElements, GetCanvas, GetMainCanvas, GetUIElementUnderMouse, IsUIElement, RebuildLayout, ScrollToBottom, ScrollToLeft, ScrollToRight, ScrollToTop, SetAnchoredPosition, SetPlaceholderText, SetSizeDelta, SetToggleState, SetValueClamped, StretchToFillParent, ToggleState
- `UnityEventExtensions`: InvokeIfNotNull
- `UnityRotateExtensions`: Rotate_DegreesPerSecond, RotateAroundAxis_X, RotateAroundAxis_Y, RotateAroundAxis_Z, WithW, WithXW, WithXY, WithXYW, WithXYZ, WithXZ, WithXZW, WithYW, WithYZ, WithYZW, WithZ, WithZW
- `UnityTransformExtensions`: AddChilds, DeepSearch, DestroyAllChildren, DestroyChild, DestroyChilds, DestroyChildsWhere, DestroyFirstChild, DestroyLastChild, DistanceTo, FindChild, FindChildRecursive, FindChildren, FindChildrenRecursive, FirstChild, GetAllSiblingObjects, GetChildrenRecursive, GetNextSiblingTransform, GetPositionX, GetPositionY, GetPositionZ, GetPreviousSiblingTransform, GetRandomChild, IncLocalX, IncLocalY, IncLocalZ, LastChild, LocalPosition2D, LookAt2D, OffsetXY, OffsetXZ, OffsetYZ, Position2, Position2D, ResetPosition, SetEulerAngles, SetEulerAnglesX, SetEulerAnglesXY, SetEulerAnglesXZ, SetEulerAnglesY, SetEulerAnglesYZ, SetEulerAnglesZ, SetLocalEulerAngles, SetLocalEulerAnglesX, SetLocalEulerAnglesXY, SetLocalEulerAnglesXZ, SetLocalEulerAnglesY, SetLocalEulerAnglesYZ, SetLocalEulerAnglesZ, SetLocalPosition, SetLocalPosition2D, SetLocalPositionX, SetLocalPositionXY, SetLocalPositionXZ, SetLocalPositionY, SetLocalPositionYZ, SetLocalPositionZ, SetLocalScale, SetLocalScaleX, SetLocalScaleXY, SetLocalScaleXZ, SetLocalScaleY, SetLocalScaleYZ, SetLocalScaleZ, SetLocalX, SetLocalXY, SetLocalXYZ, SetLocalY, SetLocalZ, SetPosition2D, SetPositionX, SetPositionXY, SetPositionXZ, SetPositionY, SetPositionYZ, SetPositionZ, SetScaleX, SetScaleY, SetScaleZ, SetToNextSibling, SetToPreviousSibling, SetXY, SetXYZ, SetXZ, SetYZ, SetZ, SnapPosition
- `ValidationExtensions`: ThrowIfNull
- `Vector2Extensions`: AddX, AddY, AverageVector, ClampX, ClampY, FindCenter2D, FindClosest2D, GetClosestPointOnRay, GetClosestPointOnSegment, GetYX, InsertX, InsertY, InsertZ, InvertX, InvertY, IsUniform, MakePixelPerfect, MaxComponent, MaxComponentIndex, MinComponent, MinComponentIndex, MoveToward, OffsetXY, ScaleBy, SnapToOne, SnapValue, ToVector2Int, ToVector3, ToVector3Int, WithNegate, WithNegateX, WithNegateY
- `Vector2IntExtensions`: AddX, AddY, GetYX, InsertX, InsertY, InsertZ, InvertX, InvertY, IsUniform, MaxComponent, MinComponent, ToVector3, WithNegate, WithNegateX, WithNegateY
- `Vector3IntExtensions`: AddX, AddY, AddZ, CompareAllComponents, GetXY, GetXZ, GetXZY, GetYX, GetYXZ, GetYZ, GetYZX, GetZX, GetZXY, GetZY, GetZYX, InvertX, InvertY, InvertZ, IsUniform, MaxComponent, MinComponent, ToVector3, WithNegate, WithNegateX, WithNegateXY, WithNegateXZ, WithNegateY, WithNegateYZ, WithNegateZ, WithXY, WithXZ, WithYZ, WithZ
- `Vector4Extensions`: ScaleBy
- `VectorExtensions`: AddX, AddY, AddZ, AngleAroundAxis, ApplyTransform, Approx, AverageVector, BytesToString, ClampX, ClampY, CompareAllComponents, CreateValidated, Distance2D, DistanceSqr2D, DistanceTo, EventlyDistributedPointOnCircle, EventlyDistributedPointOnSphere, FindClosest, GetAnglesTo, GetClosest, GetClosestPointOnRay, GetClosestPointOnSegment, GetXY, GetXZ, GetXZY, GetYX, GetYXZ, GetYZ, GetYZX, GetZX, GetZXY, GetZY, GetZYX, InsertW, InsertX, InsertY, InsertZ, InverseApplyTransform, InverseTransform, InvertX, InvertY, InvertZ, IsInside, IsUniform, MaxComponent, MaxComponentIndex, MidPoint, MidPointTo, MinComponent, MinComponentIndex, MoveToward, NearestPoint, NearlyEquals, OffsetXY, OffsetXZ, OffsetYZ, OneX, OneY, OneZ, ProjectOntoPlane, RelativePosTo, RotateAroundY, ScaleBy, SetXY, SetXZ, SetYZ, SetZ, SmoothDampEuler, SnapToOne, SnapValue, SqrDistance, SqrLineDistance, StringToBytes, ToStringVerbose, ToV3String, ToVector2Int, ToVector2Z, ToVector3, ToVector3Int, ToVector3XZ, Vector3Normal, WithNegate, WithNegateX, WithNegateXY, WithNegateXZ, WithNegateY, WithNegateYZ, WithNegateZ, WithXY, WithXZ, WithYZ, WithZ, WorldToScreenPointInCanvas, ZeroX, ZeroY, ZeroZ

</details>

### Storage

- **Added** `PersistentDataHandler.DeleteKey` / `IPersistentDataAdapter.DeleteKey`.
- **Fixed** the trim had removed `PersistentDataHandler.SubscribeLifecycle` (a `[RuntimeInitializeOnLoadMethod]` that flushes data on pause, focus loss and quit); restored with its handlers.

### AetherInspector

- **Changed** the fallback scope default is `FirstParty`. **Added** `InspectorXSettings.thirdPartyFolders` (default `Assets/AssetPacks`, `Assets/Plugins`): scripts under them are third-party even when they compile into `Assembly-CSharp` or carry an asmdef under `Assets/`, so asset-store packs keep Unity's inspector.

### Editor streamlining (agent-first)

- **Added** `EditorInteractivity.IsAgentDriven`: true while an AI Bridge tool call runs (shared `SessionState` key, no assembly reference either way). Editor code checks it before opening popups, menus, modal dialogs or moving the selection.
- **Added** `StaleComponentValidator` (Project scope): stale serialized data in Validate Project / `core-validate`; the strip fix is `RequiresChoice`.
- **Added** `feature:` Unity Search provider over the feature catalog.
- **Changed** Feature catalog ownership: any `[MenuItem]` declared in a first-party assembly (asmdef under `Packages/com.aethernexus.*` or `Assets/` outside AssetPacks/Plugins/Libraries, or asmdef-less game scripts), replacing the hardcoded menu-root allowlist.
- **Changed** EditorEnhancerX key bindings are Unity `[Shortcut]`s ("EditorEnhancerX/..." in Edit ▸ Shortcuts); off-by-default shortcuts ship unbound. Bindings are now per-user Shortcut Manager profiles, not `ProjectSettings`. **Removed** `KeyRouter`, `ShortcutBinding`, `GlobalKeyCapture` (reflection into `EditorApplication.globalEventHandler`), `ReflectionGuard`, Drop-to-Tab, the Maximize shortcut (Unity's own Shift+Space), settings JSON export/import.
- **Changed** HierarchyX row caches ignore property edits and limit Play-mode clears to one per 0.5 s; ProjectWindowX repaints on mouse move instead of every editor frame while hovered.
- **Changed** UI Validation keeps the layering/composition checks; **removed** folder-placement and suffix rules (UIV001, UIV002, UIV006, UIV012) and its import hook.
- **Removed** the Image To String Converter window (`ImageStringConverter` stays).

### Added

- **`DebugDrawKit.CaptureHeadless(draw)`** / **`DebugDrawKit.Headless`**: runs a debug section outside an IMGUI pass and returns its recorded text (used by the `core-report-game-state` agent tool). Every helper records and skips drawing while headless; sections that draw raw GUI must skip it too.
- **`DesignerFeatures.MenuPathFor(title)` / `Describe(title)`**: title lookup into the Feature Finder index for hints; a stale title throws.
- **`AuthoringValidatorRegistry.PackageIds()`**: the package ids validators declare.
- **`ProjectContentConfig.MergeLegacyValues`**: union-merge used by Migrate To v2.

- **`[ContentHome]`** (`AetherNexus.FoundationPlatform.Attributes.ContentHomeAttribute`): declares where an authored type's assets belong — folder patterns relative to `Assets/Content` (`*`, `**`, leading `***`), inherited by subclasses. It replaces `PackageIntegrationManifest` type-mapping rows as the source of truth. `ContentHomes` (editor) is the one resolver. `AnimationSet`, `AnimationSetValidationProfile`, `LocomotionBlendProfile` and `LightmapConfiguration` carry their former manifest patterns.
- **`ProjectContentConfig`** (editor): the single per-project content layout asset — content areas, combined areas, Shared/Global subfolders, required roots, exempt folders, auto-move-on-import flag.

### Removed

- `DataFolderMappingConfig`, `DataFolderExemptionMarker` (exempt folders are now a `ProjectContentConfig` list), `DataFolderMappingExemptionResolver`, `HierarchyPathPolicy.TryClassify` and the `HierarchyRoot` / `HierarchyBucket` / `DataFolderMappingPathClassification` enums. **Breaking**: move any exemption-marker folders into `ProjectContentConfig ▸ Exempt Folders`.

### Changed

- **Logging namespace renamed** `AetherNexus.FoundationPlatform.DebugX` -> `AetherNexus.FoundationPlatform.Logging` (and `.DebugX.ConsoleView.Editor` -> `.Logging.ConsoleView.Editor`); the `DebugX` class keeps its name. The old namespace shared the class's simple name, so from sibling namespaces `DebugX.Logger(...)` resolved to the namespace and needed `DebugX.DebugX.Logger(...)` or a local alias. **Breaking for consumers**: replace `using AetherNexus.FoundationPlatform.DebugX;` with `using AetherNexus.FoundationPlatform.Logging;` and drop any `DebugX.DebugX.` qualification or `using DebugX = DebugX.DebugX;` alias.
- Agent tools: `platform-agenttools-export-params` and `AgentToolParamExporter` moved into the bridge package (`com.aethernexus.aibridge`, tool `editor-export-mcp-params`, runs after every compile). The agent-tools assembly is now gated on `com.aethernexus.aibridge`.

### Removed

- `Tools/Platform/Agent Tools/Export MCP Params` menu (now **Tools > AI Bridge > Export MCP Params**)
- Publisher-only editor tooling moved to the monorepo package `com.aethernexus.publishertools` (not shipped): `Editor/DesignerIcons/`, `Editor/ComponentMenus/`, `Editor/CreateMenus/`, `Editor/DesignerSurfaces/` and their **Tools > Platform > Icons / Component Menus / Create Menus** and **Tools > Linting > Report Missing …** menus. `[DesignerIcon]` and the icon PNGs stay here.

- **Cysharp UniTask is no longer embedded** (`Runtime/ThirdParty/UniTask/`, `Editor/ThirdParty/UniTask/`, the `UniTask` and `UniTask.Editor` assemblies, and the UniTask Tracker window). Async code across the AetherNexus packages now targets `UnityEngine.Awaitable`. **Breaking for consumers** that referenced the `UniTask` assembly or typed against `UniTask` / `UniTask<T>` in overrides of package APIs — change those signatures to `Awaitable` / `Awaitable<T>` and drop the assembly reference. Fire-and-forget `UniTaskVoid` + `.Forget()` becomes `async void` with an explicit `try`/`catch`, since `Awaitable` has no unobserved-exception routing
- Legacy `UnityEngine.Input` fallbacks in shipped components: every polling site now goes through the Input System package, which the package already required

### Added

- **Agent tools** (`Editor/AgentTools/`) — the designer feature index made drivable by coding agents through the Unity AI Bridge: `platform-capability-list` (every `[DesignerFeature]` / owned `[MenuItem]` with intent-first title, blurb and keywords), `platform-capability-invoke` (runs a catalogued entry through its own action and returns what Unity logged), and `platform-agenttools-export-params` (regenerates the MCP schema files from the live tool registry, preserving hand-curated text via a `_generated.json` baseline). New public `FeatureCatalogApi` over the previously internal `FeatureCatalog`. Own assembly gated on `com.aibridge.unity`, so the package ships bridge-free. See [Editor/AgentTools/README.md](Editor/AgentTools/README.md)
- **`RandomX` gains a `UnityEngine.Random`-shaped facade** (`Extensions/Random/RandomX.Unity.cs`): `value`, `Range`, `insideUnitCircle`, `insideUnitSphere`, `onUnitSphere`, `rotation`, `rotationUniform`, `ColorHSV`, plus `Stream(name)` and `CaptureState`/`RestoreState`. Routed through an installed `RandomX.Provider`; throws with a fix message when none is installed (no silent fallback to a non-deterministic source). Member names match Unity's exactly so adoption is a mechanical `Random.` → `RandomX.` substitution
- **`IRandomStreamSource : IRandomProvider`** (`Runtime/Behaviours/`) — adds independent named streams and opaque state capture/restore, so the package can expose those without knowing any particular RNG implementation
- **`IWorldDebugSection` + `GameStateWindow`** (`Editor/Debugging/`) — world-scope counterpart to the per-entity `IEntityDebugSection` overlay: a dockable shell with TypeCache auto-discovery, live repaint, `DebugDrawKit`-backed Copy Info, and a handoff button to the Scene View overlay. Ships no gameplay sections; installed gameplay packages contribute them

### Changed

- `RandomX` is now `partial` so the collection helpers and the new facade share one type name — gameplay code should never have to choose between two "random" types
- Editor windows (`EventPublishHistoryWindow`, `ActiveSubscriptionsWindow`, `SubscriptionHistoryWindow`, `AnimationTestBenchWindow`, `AutoBinderWindow`, `SceneSwitcherWindow`, `ScriptGeneratorWindow`, `StaleComponentWindow`, `PresetAutomationWindow`, `GameStateWindow`) retrofitted onto the `GuiKit` shared chrome instead of ad-hoc `HelpBox`/`Foldout`/`GUILayout.Toolbar` calls
- `PresetAutomationWindow` now actually uses its `_scroll` field — Filters and Folder Priorities are dynamic-length lists that were previously drawn with no scroll view, clipping content taller than the window with no way to reach it

## [1.0.0] - 2026-07-14

### Added

- First public Asset Store UPM release of Foundation Platform (`com.aethernexus.foundationplatform`)
- EventBus, DebugX, CoroutineX, TweenX / Feedbacks, patterns, Identity, animation tooling, extensions
- Framework Inspector and related Editor tooling
- Package Manager sample: **EventBus + CoroutineX**
- User docs: README, Documentation index, Architecture, TweenX, Framework Inspector guides

### Notes

- Requires Unity **6000.3.10f1+**; URP recommended; Input System + uGUI
- Embeds Cysharp **UniTask 2.5.11** (MIT) — see Third-Party Notices.txt; do not install UniTask separately
- Fast Enter Play Mode (Domain Reload off) is **not supported**
- Publisher: [AetherNexus](https://aethernexus.online) · Support: wajahatdeveloperqs@gmail.com
