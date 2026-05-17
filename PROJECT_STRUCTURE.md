# Retruxel - Project Structure Analysis

Generated: 2026-05-16 10:15:38

## PROJECT: Plugins

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins

Files: 158

### FILE: AnimationPreprocessorTool.cs

Types:
  - AnimationPreprocessorTool
  - AnimationClip

Methods: 4
  - Execute()
  - GenerateEnumEntries()
  - GenerateFrameArrays()
  - Validate()

### FILE: AssetImporter.cs

Types:
  - AssetImporter
  - AssetImportException

Methods: 4
  - AssetImportException()
  - Import()
  - ReduceColorsToHardware()
  - ValidateDimensions()

### FILE: AssetImporterWindow.xaml.cs

Types:
  - AssetImporterWindow

Methods: 24
  - ApplyLocalization()
  - BtnBrowse_Click()
  - BtnCancel_Click()
  - BtnCaptureEmulator_Click()
  - BtnClose_Click()
  - BtnImport_Click()
  - ClearValidation()
  - CountUniqueColors()
  - DropZone_DragOver()
  - DropZone_Drop()
  - GenerateReducedPreview()
  - GenerateRegionControls()
  - GetSelectedRegionId()
  - LoadSourceImage()
  - OnClosed()
  - PreSelectRegion()
  - ProcessEmulatorCapture()
  - RbSource_Changed()
  - ShowPaletteImportDialog()
  - ShowValidation()
  - TitleBar_MouseLeftButtonDown()
  - TxtAssetName_TextChanged()
  - UpdateImportButton()
  - ValidateAssetName()

### FILE: AssetProcessorTool.cs

Types:
  - AssetProcessorTool

Methods: 3
  - ApplyGenerationParams()
  - Execute()
  - LoadNormalizedBitmap()

### FILE: AudioEditorTool.cs

Types:
  - AudioEditorTool

Methods: 1
  - Execute()

### FILE: AutoPortingTool.cs

Types:
  - AutoPortingTool

Methods: 1
  - Execute()

### FILE: BankManagerTool.cs

Types:
  - BankManagerTool

Methods: 1
  - Execute()

### FILE: BdfParser.cs

Types:
  - BdfParser

Methods: 2
  - Parse()
  - ReverseBits()

### FILE: BgbConnection.cs

Types:
  - BgbConnection

Methods: 7
  - ConnectAsync()
  - DisconnectAsync()
  - GetStateAsync()
  - ParseByteResponse()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SendCommandAsync()

### FILE: CanvasExpansionCapture.cs

Types:
  - CanvasExpansionCapture
  - ExpansionDirection
  - ExpansionResult
  - SearchResult

Methods: 4
  - EstimateMapHeight()
  - ExpandCanvas()
  - SearchFullNametableInRom()
  - SearchNametableInData()

### FILE: CanvasExpansionWindow.xaml.cs

Types:
  - CanvasExpansionWindow

Methods: 5
  - BtnCancel_Click()
  - BtnClose_Click()
  - BtnExpand_Click()
  - TitleBar_MouseLeftButtonDown()
  - UpdatePreview()

### FILE: CaptureResult.cs

Types:
  - CaptureResult

### FILE: CaptureToImportedAssetPipeline.cs

Types:
  - CaptureToImportedAssetPipeline

Methods: 1
  - ProcessTyped()

### FILE: CodeAnalyzerTool.cs

Types:
  - CodeAnalyzerTool

Methods: 1
  - Execute()

### FILE: CodeGenGenerator.cs

Types:
  - CodeGenGenerator

Methods: 4
  - Generate()
  - GenerateCodeGenJson()
  - GenerateCodeTemplate()
  - GenerateReadme()

### FILE: CodeGenStep1BasicInfo.xaml.cs

Types:
  - CodeGenStep1BasicInfo

Methods: 4
  - AttachHandlers()
  - InstallTarget_Click()
  - LoadData()
  - LoadTargets()

### FILE: CodeGenStep2Code.xaml.cs

Types:
  - CodeGenStep2Code

Methods: 4
  - AttachHandlers()
  - Browse_Click()
  - LoadData()
  - SourceComboBox_SelectionChanged()

### FILE: CodeGenStep3VariableMapping.xaml.cs

Types:
  - CodeGenStep3VariableMapping
  - CodeGenVariableMappingItem

Methods: 4
  - DetectVariables()
  - GetDefaultJsonPath()
  - GetDefaultType()
  - OnPropertyChanged()

### FILE: CodeGenStep4Dependencies.xaml.cs

Types:
  - CodeGenStep4Dependencies

Methods: 2
  - LoadTools()
  - UpdateSelectedToolsDisplay()

### FILE: CodeGenWizardWindow.xaml.cs

Types:
  - CodeGenWizardWindow

Methods: 8
  - Back_Click()
  - Cancel_Click()
  - GenerateCodeGen()
  - InitializeSteps()
  - Next_Click()
  - ShowStep()
  - TitleBar_MouseLeftButtonDown()
  - ValidateCurrentStep()

### FILE: ColecoTilePackerExtension.cs

Types:
  - ColecoTilePackerExtension

Methods: 1
  - Execute()

### FILE: ColecoVisionTarget.cs

Types:
  - ColecoVisionTarget

Methods: 11
  - GenerateCodeForModule()
  - GenerateMainFile()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetModuleOverrides()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()

### FILE: CollaborationTool.cs

Types:
  - CollaborationTool

Methods: 1
  - Execute()

### FILE: ColorMatching.cs

Types:
  - ColorMatching
  - DistanceMode
  - LabColor
  - RgbColor
  - FastColor

Methods: 8
  - BitmapFromByteArray()
  - ColorDistance()
  - FindNearestCentroid()
  - FindNearestColorIndex()
  - LabF()
  - OptimizePalette()
  - QuantizePalette()
  - RgbToLinear()

### FILE: CompressionEngineTool.cs

Types:
  - CompressionEngineTool

Methods: 1
  - Execute()

### FILE: CompressionStubs.cs

Error reading file

### FILE: ConstraintAnalyzerTool.cs

Types:
  - ConstraintAnalyzerTool

Methods: 1
  - Execute()

### FILE: CreationAssistantTool.cs

Types:
  - CreationAssistantTool

Methods: 1
  - Execute()

### FILE: EmuliciousConnection.cs

Types:
  - EmuliciousConnection

Methods: 6
  - ConnectAsync()
  - DisconnectAsync()
  - GetStateAsync()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SendCommandAsync()

### FILE: FontImporterWindow.xaml.cs

Types:
  - FontImporterWindow

Methods: 22
  - Antialiasing_Changed()
  - BrowseButton_Click()
  - BuildCell()
  - BuildCharGrid()
  - ClearSelection_Click()
  - CloseButton_Click()
  - FontSize_Changed()
  - ImportButton_Click()
  - Offset_Changed()
  - RefreshCellStyle()
  - RegenerateAllGlyphs()
  - SelectAll_Click()
  - SelectAsciiBasic_Click()
  - SelectAsciiBasicRange()
  - SizePreset_Click()
  - TileSize_Changed()
  - TitleBar_MouseLeftButtonDown()
  - ToggleCell()
  - UpdateGlyphPreview()
  - UpdateImportButton()
  - UpdateSheetPreview()
  - UpdateStats()

### FILE: FontImportResult.cs

Types:
  - FontImportResult

### FILE: FontRasterizer.cs

Types:
  - FontRasterizer

Methods: 4
  - BuildFont()
  - BuildPaint()
  - DrawGlyph()
  - RenderSpritesheet()

### FILE: GgTarget.cs

Types:
  - GgTarget

Methods: 11
  - GenerateCodeForModule()
  - GenerateMainFile()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetModuleOverrides()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()

### FILE: HardwareSimulatorTool.cs

Types:
  - HardwareSimulatorTool

Methods: 1
  - Execute()

### FILE: HitboxDefinition.cs

Types:
  - HitboxDefinition
  - HitboxType

### FILE: HitboxTypeDialog.xaml.cs

Types:
  - HitboxTypeDialog

Methods: 2
  - BtnAdd_Click()
  - BtnCancel_Click()

### FILE: HybridCodeEditorTool.cs

Types:
  - HybridCodeEditorTool

Methods: 1
  - Execute()

### FILE: ImageProcessing.cs

Types:
  - ImageProcessing

Methods: 2
  - BitmapSourceBitmapToSkia()
  - ConvertSkBitmapToBitmapSource()

### FILE: ImportedAssetToTilemapPipeline.cs

Types:
  - ImportedAssetToTilemapPipeline

Methods: 12
  - CalculateTilesetHeight()
  - CalculateTilesetWidth()
  - CalculateTilesPerRow()
  - DrawTileToBitmap()
  - FindClosestHardwareColor()
  - GenerateUniqueAssetId()
  - LabF()
  - ProcessTyped()
  - SaveBitmapDirectly()
  - SaveTilesAsAsset()
  - SaveTilesReconstructed()
  - SrgbToLinear()

### FILE: IndexedBitmapRenderer.cs

Types:
  - is
  - IndexedBitmapRenderer

Methods: 3
  - ExtractPixels()
  - Render()
  - RenderTile()

### FILE: IndexedPngService.cs

Types:
  - IndexedPngService
  - IndexedPngData

Methods: 3
  - FindNearestIndex()
  - RenderPreview()
  - Write()

### FILE: IntelligenceEngineTool.cs

Types:
  - IntelligenceEngineTool

Methods: 1
  - Execute()

### FILE: LiveLinkSpriteCaptureDialog.xaml.cs

Types:
  - LiveLinkSpriteCaptureDialog

Methods: 4
  - BtnCancel_Click()
  - BtnCapture_Click()
  - TxtTileCount_TextChanged()
  - UpdateVramSize()

### FILE: LiveLinkTool.cs

Types:
  - LiveLinkTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: LiveLinkWindow.xaml.cs

Types:
  - file
  - files
  - LiveLinkWindow

### FILE: LiveLinkWindow_Capture.cs

Types:
  - LiveLinkWindow
  - paletteData

Methods: 16
  - BtnCaptureScreen_Click()
  - BtnCaptureVRAM_Click()
  - CaptureGameBoyPalette()
  - CaptureGameBoyTiles()
  - CaptureGameGearPalette()
  - CaptureNesNametable()
  - CaptureNesPalette()
  - CaptureNesTiles()
  - CaptureSg1000Nametable()
  - CaptureSg1000Palette()
  - CaptureSg1000Tiles()
  - CaptureSmsNametable()
  - CaptureSmsPalette()
  - CaptureSmsTiles()
  - CaptureSnesPalette()
  - CaptureSnesTiles()

### FILE: LiveLinkWindow_Connection.cs

Types:
  - LiveLinkWindow

Methods: 6
  - BtnConnect_Click()
  - DetectConsoleFromRom()
  - DiscoverEmulators()
  - GetDebugApiInstructions()
  - KeepAliveCheck()
  - StartKeepAlive()

### FILE: LiveLinkWindow_ExpandCanvas.cs

Types:
  - LiveLinkWindow

Methods: 1
  - BtnExpandCanvas_Click()

### FILE: LiveLinkWindow_Import.cs

Types:
  - LiveLinkWindow

Methods: 4
  - ApplyOptimizedPaletteToCapture()
  - BtnImport_Click()
  - ConvertBitmapToCapture()
  - ExtractPixelsFromBitmap()

### FILE: LiveLinkWindow_Main.cs

Types:
  - with
  - LiveLinkWindow

Methods: 1
  - OnWindowClosing()

### FILE: LiveLinkWindow_Palette.cs

Types:
  - LiveLinkWindow

Methods: 1
  - BtnValidateSmsColors_Click()

### FILE: LiveLinkWindow_Preview.cs

Types:
  - LiveLinkWindow

Methods: 2
  - RenderPreview()
  - RenderTilesInGrid()

### FILE: LiveLinkWindow_Specs.cs

Types:
  - LiveLinkWindow

### FILE: LiveLinkWindow_UI.cs

Types:
  - LiveLinkWindow

Methods: 9
  - AppendLog()
  - BtnClearLog_Click()
  - BtnClose_Click()
  - BtnCopyLog_Click()
  - LogError()
  - LogInfo()
  - LogSuccess()
  - LogWarning()
  - TitleBar_MouseLeftButtonDown()

### FILE: MarketingKitTool.cs

Types:
  - MarketingKitTool

Methods: 1
  - Execute()

### FILE: MesenConnection.cs

Types:
  - MesenConnection

Methods: 10
  - ConnectAsync()
  - DisconnectAsync()
  - GetScreenBufferAsync()
  - GetStateAsync()
  - Log()
  - PingAsync()
  - ReadCramAsync()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SetLogCallback()

### FILE: MesenSConnection.cs

Types:
  - MesenSConnection

Methods: 6
  - ConnectAsync()
  - DisconnectAsync()
  - GetStateAsync()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SendCommandAsync()

### FILE: MetaspritePreprocessorTool.cs

Types:
  - MetaspritePreprocessorTool
  - TileEntry

Methods: 3
  - Execute()
  - FormatIntArray()
  - GetInt()

### FILE: MgbaConnection.cs

Types:
  - MgbaConnection

Methods: 6
  - ConnectAsync()
  - DisconnectAsync()
  - GetStateAsync()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SendGdbCommandAsync()

### FILE: NametableDecoder.cs

Types:
  - NametableDecoder

### FILE: NesTarget.cs

Types:
  - NesTarget

Methods: 11
  - GenerateCodeForModule()
  - GenerateMainFile()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetModuleOverrides()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()

### FILE: NesTilePackerExtension.cs

Types:
  - NesTilePackerExtension

Methods: 1
  - Execute()

### FILE: PaletteEditorTool.cs

Types:
  - PaletteEditorTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: PaletteEditorWindow.xaml.cs

Types:
  - PaletteEditorWindow
  - TargetPaletteProvider

Methods: 16
  - AddUsageText()
  - Apply()
  - ApplyLocalization()
  - BuildHardwareColorGrid()
  - BuildSlotsGrid()
  - CreateNewPalette()
  - DeletePalette()
  - DuplicatePalette()
  - GetColorFormat()
  - InitializeUI()
  - LoadDefaultPalette()
  - OnClosed()
  - RefreshSlots()
  - SelectSlot()
  - SetSlotColor()
  - UpdateUsageReport()

### FILE: PaletteExtractor.cs

Types:
  - PaletteExtractor

### FILE: PaletteHelpers.cs

Types:
  - PaletteHelpers

Methods: 6
  - FindClosestHardwareColor()
  - OpenPaletteEditorForSlot()
  - ParseSlotIndexFromComboBoxItem()
  - PopulatePaletteSlotComboBox()
  - ResolvePaletteColors()
  - SavePaletteSlotToModuleData()

### FILE: PaletteImportDialog.xaml.cs

Types:
  - PaletteImportDialog

Methods: 7
  - BtnCancel_Click()
  - BtnOk_Click()
  - BuildColorPreview()
  - BuildSlotOptions()
  - BuildTransparentColorSelector()
  - MakeSwatch()
  - UpdateTransparentColorSelection()

### FILE: PaletteImportResult.cs

Types:
  - PaletteImportResult

### FILE: PaletteMapper.cs

Types:
  - PaletteMapper

Methods: 3
  - FindClosestHardwareColor()
  - LabF()
  - RgbToLinear()

### FILE: PaletteOptimizationWindow.xaml.cs

Types:
  - PaletteOptimizationWindow

Methods: 12
  - ApplyPalette()
  - BtnCancel_Click()
  - BtnOk_Click()
  - ColorSpace_Changed()
  - ConfirmPalette()
  - RefreshPaletteSwatches()
  - RgbToHue()
  - RgbToLightness()
  - RgbToSaturation()
  - SliderDiversity_ValueChanged()
  - SortPalette()
  - UpdateOptimizedPreview()

### FILE: PixelArtEditorTool.cs

Types:
  - PixelArtEditorTool

Methods: 1
  - Execute()

### FILE: PlaytestBotTool.cs

Types:
  - PlaytestBotTool

Methods: 1
  - Execute()

### FILE: PngReader.cs

Types:
  - PngReader

Methods: 1
  - LoadBitmap()

### FILE: PngToTilesTool.cs

Types:
  - PngToTilesTool

Methods: 3
  - Execute()
  - GetDefaultParameters()
  - GetInt()

### FILE: ProcessedTileEntry.cs

Types:
  - ProcessedTileEntry

### FILE: ScreenToTilesConverter.cs

Types:
  - ScreenToTilesConverter
  - ConversionResult
  - OptimizedPalette

Methods: 1
  - Convert()

### FILE: ScriptExtractor.cs

Types:
  - ScriptExtractor

Methods: 2
  - ExtractScript()
  - GetScriptPath()

### FILE: SG1000Target.cs

Types:
  - SG1000Target

Methods: 11
  - GenerateCodeForModule()
  - GenerateMainFile()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetModuleOverrides()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()

### FILE: SmsColorUtils.cs

Types:
  - SmsColorUtils

Methods: 2
  - ConvertFromSmsRgb222()
  - ConvertToSmsRgb222()

### FILE: SmsColorValidator.cs

Types:
  - SmsColorValidator

Methods: 2
  - ExtractPixels()
  - GenerateSmsMasterPalette()

### FILE: SmsDiagnosticsProvider.cs

Types:
  - SmsDiagnosticsProvider

Methods: 5
  - Analyze()
  - CountModules()
  - CountSourceLines()
  - CountTiles()
  - EstimateRamUsage()

### FILE: SmsFontConverter.cs

Types:
  - SmsFontConverter

Methods: 1
  - ReverseBits()

### FILE: SmsPaletteConverter.cs

Types:
  - SmsPaletteConverter

### FILE: SmsPaletteEditorExtension.cs

Types:
  - SmsPaletteEditorExtension

Methods: 2
  - Execute()
  - GetColorFormat()

### FILE: SmsPngToTilesExtension.cs

Types:
  - SmsPngToTilesExtension

Methods: 2
  - Execute()
  - GetDefaultParameters()

### FILE: SmsRenderBackend.cs

Types:
  - is
  - SmsRenderBackend

Methods: 3
  - GenerateEngineHeader()
  - GenerateEngineSource()
  - GetConstraints()

### FILE: SmsSplashCodeGen.cs

Types:
  - SmsSplashCodeGen

Methods: 2
  - GenerateCode()
  - GenerateHeader()

### FILE: SmsTarget.cs

Types:
  - SmsTarget

Methods: 19
  - GenerateCodeForModule()
  - GenerateEngineRuntime()
  - GenerateMainFile()
  - GenerateSceneTransitionPostamble()
  - GenerateSceneTransitionPreamble()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetColorsPerSlot()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetHudStrategy()
  - GetMaxPalettesPerTilemap()
  - GetPaletteSlotCount()
  - GetPaletteSlotType()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()
  - InjectWarnings()

### FILE: SmsTilemapPreprocessorExtension.cs

Types:
  - SmsTilemapPreprocessorExtension
  - ProcessedTileEntry

Methods: 5
  - ConvertToSmsNametableWord()
  - Execute()
  - FormatMapAsHex()
  - GetDefaultParameters()
  - GetInt()

### FILE: SmsTilePackerExtension.cs

Types:
  - SmsTilePackerExtension

Methods: 2
  - Execute()
  - GetDefaultParameters()

### FILE: SpriteEditorTool.cs

Types:
  - SpriteEditorTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: SpriteEditorWindow.xaml.cs

Types:
  - file
  - files
  - SpriteEditorWindow

### FILE: SpriteEditorWindow_Animation.cs

Types:
  - SpriteEditorWindow

Methods: 7
  - AnimationTimer_Tick()
  - BtnPlayPause_Click()
  - InitializeAnimation()
  - RenderPreview()
  - StartAnimation()
  - StopAnimation()
  - UpdateAnimationSpeed()

### FILE: SpriteEditorWindow_Asset.cs

Types:
  - SpriteEditorWindow

Methods: 5
  - BtnBrowseTileset_Click()
  - CmbTilesetAsset_SelectionChanged()
  - InitializeAssetSelector()
  - LoadTilesetFromAsset()
  - SaveAssetSelection()

### FILE: SpriteEditorWindow_Canvas.cs

Types:
  - SpriteEditorWindow

Methods: 10
  - AddTileToCurrentFrame()
  - Canvas_DragOver()
  - Canvas_Drop()
  - CanvasTile_MouseDown()
  - CanvasTile_MouseMove()
  - CanvasTile_MouseUp()
  - DrawGrid()
  - RemoveTileFromCurrentFrame()
  - RenderCanvas()
  - UpdateStatusBar()

### FILE: SpriteEditorWindow_Frames.cs

Types:
  - SpriteEditorWindow

Methods: 7
  - BtnAddFrame_Click()
  - BtnDeleteFrame_Click()
  - BtnDuplicateFrame_Click()
  - FramesListBox_SelectionChanged()
  - RefreshFramesList()
  - TxtFrameDuration_TextChanged()
  - UpdateFrameDurationField()

### FILE: SpriteEditorWindow_Hitboxes.cs

Types:
  - SpriteEditorWindow

Methods: 4
  - BtnAddHitbox_Click()
  - BtnDeleteHitbox_Click()
  - DrawHitboxes()
  - RefreshHitboxList()

### FILE: SpriteEditorWindow_Initialization.cs

Types:
  - SpriteEditorWindow

Methods: 7
  - BtnCancel_Click()
  - BtnClose_Click()
  - BtnSave_Click()
  - InitializeUI()
  - LoadModuleData()
  - SaveModuleData()
  - TitleBar_MouseLeftButtonDown()

### FILE: SpriteEditorWindow_LiveLink.cs

Types:
  - SpriteEditorWindow

Methods: 1
  - BtnLiveLink_Click()

### FILE: SpriteEditorWindow_Main.cs

Types:
  - SpriteEditorWindow

Methods: 1
  - OnSpriteChanged()

### FILE: SpriteEditorWindow_PaletteSlot.cs

Types:
  - SpriteEditorWindow

Methods: 4
  - BtnEditPalette_Click()
  - CmbPaletteSlot_SelectionChanged()
  - InitializePaletteSlotSelector()
  - SavePaletteSlotSelection()

### FILE: SpriteEditorWindow_Tileset.cs

Types:
  - SpriteEditorWindow

Methods: 8
  - CreateTileButton()
  - ExtractTile()
  - RefreshTilesetWithPalette()
  - RenderTileset()
  - TileButton_Click()
  - TileButton_MouseMove()
  - UpdateTilesetSelection()
  - UpdateVramInfo()

### FILE: SpriteEditorWindow_Zoom.cs

Types:
  - SpriteEditorWindow

Methods: 7
  - GetCanvasDisplaySize()
  - GetCanvasZoom()
  - GetTileDisplaySize()
  - InitializeZoomControls()
  - RefreshTilesetFromAsset()
  - SetCanvasZoom()
  - SetTilesetZoom()

### FILE: SpriteFrame.cs

Types:
  - SpriteFrame

Methods: 1
  - Clone()

### FILE: SpriteRenderer.cs

Types:
  - SpriteRenderer

Methods: 3
  - CreateEmptyBitmap()
  - ExtractTile()
  - RenderFrame()

### FILE: SpriteState.cs

Types:
  - SpriteState

### FILE: SpriteTile.cs

Types:
  - SpriteTile

Methods: 1
  - Clone()

### FILE: TargetGenerator.cs

Types:
  - TargetGenerator

Methods: 7
  - Generate()
  - GenerateCsproj()
  - GenerateMainTemplate()
  - GenerateReadme()
  - GenerateTargetClass()
  - GenerateTargetJson()
  - GenerateToolchainAdapter()

### FILE: TargetStep1BasicInfo.xaml.cs

Types:
  - TargetStep1BasicInfo

Methods: 2
  - AttachHandlers()
  - LoadData()

### FILE: TargetStep2Specs.xaml.cs

Types:
  - TargetStep2Specs

Methods: 2
  - AttachHandlers()
  - LoadData()

### FILE: TargetStep3Toolchain.xaml.cs

Types:
  - TargetStep3Toolchain

Methods: 2
  - AttachHandlers()
  - LoadData()

### FILE: TargetStep4MainFile.xaml.cs

Types:
  - TargetStep4MainFile

Methods: 5
  - AttachHandlers()
  - Browse_Click()
  - GenerateDefaultTemplate()
  - LoadData()
  - SourceComboBox_SelectionChanged()

### FILE: TargetStep5VariableMapping.xaml.cs

Types:
  - TargetStep5VariableMapping
  - VariableMappingItem

Methods: 3
  - DetectVariables()
  - GetDefaultMapping()
  - OnPropertyChanged()

### FILE: TargetWizardWindow.xaml.cs

Types:
  - TargetWizardWindow

Methods: 8
  - Back_Click()
  - Cancel_Click()
  - GenerateTarget()
  - InitializeSteps()
  - Next_Click()
  - ShowStep()
  - TitleBar_MouseLeftButtonDown()
  - ValidateCurrentStep()

### FILE: TemplateLoader.cs

Types:
  - TemplateLoader

Methods: 1
  - LoadTemplate()

### FILE: TextArrayEditorTool.cs

Types:
  - TextArrayEditorTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: TextArrayEditorWindow.xaml.cs

Types:
  - file
  - files
  - TextArrayEditorWindow

### FILE: TextArrayEditorWindow_ArrayName.cs

Types:
  - TextArrayEditorWindow

Methods: 2
  - IsValidIdentifier()
  - TxtArrayNameInput_TextChanged()

### FILE: TextArrayEditorWindow_Font.cs

Types:
  - TextArrayEditorWindow
  - FontCategoryItem
  - AsciiMapItem

Methods: 10
  - BtnBrowseFont_Click()
  - BtnImportFromTtf_Click()
  - BtnLiveLink_Click()
  - CmbFontCategory_SelectionChanged()
  - DrawGridOverlay()
  - ExpandMoreFontsAsync()
  - PopulateAsciiMap()
  - PopulateFontCategories()
  - RenderFontPreview()
  - RenderRepositoryFontPreviewAsync()

### FILE: TextArrayEditorWindow_Languages.cs

Types:
  - TextArrayEditorWindow

Methods: 3
  - BtnAddLanguage_Click()
  - RefreshLanguageTabs()
  - SelectLanguage()

### FILE: TextArrayEditorWindow_Main.cs

Types:
  - with
  - TextArrayEditorWindow
  - TextArrayState
  - TextLanguage
  - TextInputDialog

### FILE: TextArrayEditorWindow_Preview.cs

Types:
  - TextArrayEditorWindow

Methods: 1
  - RenderPreview()

### FILE: TextArrayEditorWindow_Strings.cs

Types:
  - TextArrayEditorWindow

Methods: 3
  - BtnAddString_Click()
  - RefreshStringsList()
  - RemoveStringAtIndex()

### FILE: TextArrayEditorWindow_Window.cs

Types:
  - TextArrayEditorWindow

Methods: 8
  - ActivateTab()
  - BtnCancel_Click()
  - BtnClose_Click()
  - BtnSave_Click()
  - BtnTabFont_Click()
  - BtnTabStrings_Click()
  - SaveAndClose()
  - TitleBar_MouseLeftButtonDown()

### FILE: TileConverter.cs

Types:
  - TileConverter

### FILE: TileEntry.cs

Types:
  - TileEntry

### FILE: TileFormat.cs

Types:
  - TileFormat
  - InterleaveMode

### FILE: TilemapData.cs

Types:
  - TilemapData

Methods: 9
  - ClearLayer()
  - FillLayer()
  - GetTile()
  - GetTileIndex()
  - Initialize()
  - Resize()
  - SetTile()

### FILE: TilemapEditorTool.cs

Types:
  - TilemapEditorTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: TilemapEditorWindow.xaml.cs

Types:
  - ToolMode
  - TilemapEditorWindow

### FILE: TilemapEditorWindow_Actions.cs

Types:
  - TilemapEditorWindow

Methods: 9
  - BtnCancel_Click()
  - BtnClear_Click()
  - BtnClose_Click()
  - BtnFill_Click()
  - BtnImportTilesetAsMap_Click()
  - BtnSave_Click()
  - LoadFromBase64()
  - LoadModuleData()
  - TitleBar_MouseLeftButtonDown()

### FILE: TilemapEditorWindow_Assets.cs

Types:
  - TilemapEditorWindow

Methods: 2
  - BtnImportAsset_Click()
  - BtnImportFromLiveLink_Click()

### FILE: TilemapEditorWindow_Canvas.cs

Types:
  - TilemapEditorWindow

Methods: 12
  - Canvas_MouseDown()
  - Canvas_MouseLeave()
  - Canvas_MouseLeftButtonDown()
  - Canvas_MouseLeftButtonUp()
  - Canvas_MouseMove()
  - Canvas_MouseRightButtonDown()
  - Canvas_MouseUp()
  - Canvas_MouseWheel()
  - DrawViewportOverlay()
  - PaintTile()
  - RenderCanvas()
  - RenderTileAt()

### FILE: TilemapEditorWindow_Flip.cs

Types:
  - TilemapEditorWindow

Methods: 2
  - InitializeFlipHotkeys()
  - Window_KeyDown()

### FILE: TilemapEditorWindow_Initialization.cs

Types:
  - TilemapEditorWindow

Methods: 1
  - InitializeUI()

### FILE: TilemapEditorWindow_Layers.cs

Types:
  - TilemapEditorWindow

Methods: 2
  - ChkShowCollision_CheckedChanged()
  - CmbLayers_SelectionChanged()

### FILE: TilemapEditorWindow_Offset.cs

Types:
  - TilemapEditorWindow

Methods: 5
  - BtnOffsetDown_Click()
  - BtnOffsetLeft_Click()
  - BtnOffsetRight_Click()
  - BtnOffsetUp_Click()
  - UpdateMapOffset()

### FILE: TilemapEditorWindow_PaletteOptimization.cs

Types:
  - TilemapEditorWindow

Methods: 3
  - OpenColorOptimizationForAsset()
  - ReduceColorsToHardware()
  - SaveSkBitmapToFile()

### FILE: TilemapEditorWindow_PaletteSlot.cs

Types:
  - TilemapEditorWindow

Methods: 6
  - BtnEditPalette_Click()
  - CmbPalette_DropDownClosed()
  - CmbPalette_SelectionChanged()
  - InitializePaletteSlotSelector()
  - OpenPaletteEditor()
  - SavePaletteSlotSelection()

### FILE: TilemapEditorWindow_Selection.cs

Types:
  - TilemapEditorWindow

Methods: 12
  - GetTilePosition()
  - HidePaintPreview()
  - InitializeSelection()
  - PlaceTileBlock()
  - RenderTileBlock()
  - ShowPaintPreview()
  - TileButton_MouseDown()
  - TileButton_MouseMove()
  - TileButton_MouseUp()
  - UpdateRectangularSelection()
  - UpdateSelectedTilePreview()
  - UpdateTileSelectionVisual()

### FILE: TilemapEditorWindow_Tileset.cs

Types:
  - TilemapEditorWindow

Methods: 20
  - CmbTilesetAsset_SelectionChanged()
  - DrawTilesetGrid()
  - FindClosestHardwareColor()
  - HitTestTile()
  - LoadAssets()
  - LoadTilesetImage()
  - RebuildTilesetBitmap()
  - RefreshTilesetFromAsset()
  - RenderTilesetCanvas()
  - ResolvePaletteColors()
  - SetTilesetGridColor()
  - TilesetCanvas_MouseLeftButtonDown()
  - TilesetCanvas_MouseLeftButtonUp()
  - TilesetCanvas_MouseMiddleButtonDown()
  - TilesetCanvas_MouseMiddleButtonUp()
  - TilesetCanvas_MouseMove()
  - TilesetCanvas_MouseWheel()
  - TilesetScrollViewer_PreviewMouseWheel()
  - ToggleTilesetGrid()
  - UpdateTileselectionOverlay()

### FILE: TilemapEditorWindow_Tools.cs

Types:
  - TilemapEditorWindow

Methods: 5
  - ApplyOptimization()
  - BtnExportPng_Click()
  - BtnOptimize_Click()
  - CreateOptimizedTileset()
  - UpdateAssetWithOptimization()

### FILE: TilemapEditorWindow_Zoom.cs

Types:
  - TilemapEditorWindow

Methods: 21
  - AddMenuItem()
  - BtnCanvasZoom_Click()
  - BtnTilesetZoom_Click()
  - HandleCanvasMouseWheel()
  - HandleCanvasPanEnd()
  - HandleCanvasPanMove()
  - HandleCanvasPanStart()
  - HandleTilesetMouseWheel()
  - HandleTilesetPanEnd()
  - HandleTilesetPanMove()
  - HandleTilesetPanStart()
  - OnKeyDown()
  - ScrollToOrigin()
  - SetZoom()
  - ShowZoomMenu()
  - StepZoom()
  - UpdateCanvasZoomLabel()
  - UpdateTilesetZoomLabel()
  - ZoomCanvasAtPoint()
  - ZoomTilesetAtPoint()
  - ZoomToFit()

### FILE: TilemapPreprocessorTool.cs

Types:
  - TilemapPreprocessorTool

Methods: 3
  - Execute()
  - GetInt()
  - Validate()

### FILE: TilemapSerializer.cs

Types:
  - TilemapSerializer

Methods: 1
  - ToBase64()

### FILE: TilePackerTool.cs

Types:
  - TilePackerTool

Methods: 3
  - ComputeHash()
  - Execute()
  - PackTiles()

### FILE: TilesetLayoutCalculator.cs

Types:
  - TilesetLayoutCalculator

Methods: 3
  - CalculateTilesetHeight()
  - CalculateTilesetWidth()
  - CalculateTilesPerRow()

### FILE: TilesetRenderer.cs

Types:
  - TilesetRenderer

Methods: 3
  - ApplyTransform()
  - LoadFromBitmap()
  - LoadTileset()

### FILE: TileSlicer.cs

Types:
  - TileSlicer

### FILE: ToolGenerator.cs

Types:
  - ToolGenerator

Methods: 6
  - Execute()
  - Generate()
  - GenerateCsproj()
  - GenerateReadme()
  - GenerateToolClass()
  - Validate()

### FILE: ToolStep1BasicInfo.xaml.cs

Types:
  - ToolStep1BasicInfo

Methods: 2
  - AttachHandlers()
  - LoadData()

### FILE: ToolStep2Configuration.xaml.cs

Types:
  - ToolStep2Configuration

Methods: 3
  - AttachHandlers()
  - LoadData()
  - LoadTargets()

### FILE: ToolStep3Parameters.xaml.cs

Types:
  - ToolStep3Parameters
  - ToolParameterItem

Methods: 5
  - AddParameter_Click()
  - LoadParameters()
  - OnParameterChanged()
  - OnPropertyChanged()
  - RemoveParameter_Click()

### FILE: ToolWizardWindow.xaml.cs

Types:
  - ToolWizardWindow

Methods: 8
  - Back_Click()
  - Cancel_Click()
  - GenerateTool()
  - InitializeSteps()
  - Next_Click()
  - ShowStep()
  - TitleBar_MouseLeftButtonDown()
  - ValidateCurrentStep()

### FILE: VisualScriptingTool.cs

Types:
  - VisualScriptingTool

Methods: 1
  - Execute()

### FILE: WizardData.cs

Types:
  - TargetWizardData
  - CodeGenWizardData
  - VariableMapping
  - ToolWizardData
  - ToolParameter

### FILE: WizardMainWindow.xaml.cs

Types:
  - WizardMainWindow

Methods: 5
  - Close_Click()
  - CreateCodeGen_Click()
  - CreateTarget_Click()
  - CreateTool_Click()
  - TitleBar_MouseLeftButtonDown()

### FILE: WizardTool.cs

Types:
  - WizardTool

Methods: 2
  - Execute()
  - Validate()

---

## PROJECT: Retruxel.Target.ColecoVision

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Targets\Retruxel.Target.ColecoVision

Files: 2

### FILE: ColecoTilePackerExtension.cs

Types:
  - ColecoTilePackerExtension

Methods: 1
  - Execute()

### FILE: ColecoVisionTarget.cs

Types:
  - ColecoVisionTarget

Methods: 11
  - GenerateCodeForModule()
  - GenerateMainFile()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetModuleOverrides()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()

---

## PROJECT: Retruxel.Target.GG

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Targets\Retruxel.Target.GG

Files: 1

### FILE: GgTarget.cs

Types:
  - GgTarget

Methods: 11
  - GenerateCodeForModule()
  - GenerateMainFile()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetModuleOverrides()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()

---

## PROJECT: Retruxel.Target.NES

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Targets\Retruxel.Target.NES

Files: 2

### FILE: NesTarget.cs

Types:
  - NesTarget

Methods: 11
  - GenerateCodeForModule()
  - GenerateMainFile()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetModuleOverrides()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()

### FILE: NesTilePackerExtension.cs

Types:
  - NesTilePackerExtension

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Target.SG1000

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Targets\Retruxel.Target.SG1000

Files: 1

### FILE: SG1000Target.cs

Types:
  - SG1000Target

Methods: 11
  - GenerateCodeForModule()
  - GenerateMainFile()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetModuleOverrides()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()

---

## PROJECT: Retruxel.Target.SMS

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Targets\Retruxel.Target.SMS

Files: 11

### FILE: SmsColorUtils.cs

Types:
  - SmsColorUtils

Methods: 2
  - ConvertFromSmsRgb222()
  - ConvertToSmsRgb222()

### FILE: SmsDiagnosticsProvider.cs

Types:
  - SmsDiagnosticsProvider

Methods: 5
  - Analyze()
  - CountModules()
  - CountSourceLines()
  - CountTiles()
  - EstimateRamUsage()

### FILE: SmsFontConverter.cs

Types:
  - SmsFontConverter

Methods: 1
  - ReverseBits()

### FILE: SmsPaletteConverter.cs

Types:
  - SmsPaletteConverter

### FILE: SmsPaletteEditorExtension.cs

Types:
  - SmsPaletteEditorExtension

Methods: 2
  - Execute()
  - GetColorFormat()

### FILE: SmsPngToTilesExtension.cs

Types:
  - SmsPngToTilesExtension

Methods: 2
  - Execute()
  - GetDefaultParameters()

### FILE: SmsRenderBackend.cs

Types:
  - is
  - SmsRenderBackend

Methods: 3
  - GenerateEngineHeader()
  - GenerateEngineSource()
  - GetConstraints()

### FILE: SmsSplashCodeGen.cs

Types:
  - SmsSplashCodeGen

Methods: 2
  - GenerateCode()
  - GenerateHeader()

### FILE: SmsTarget.cs

Types:
  - SmsTarget

Methods: 19
  - GenerateCodeForModule()
  - GenerateEngineRuntime()
  - GenerateMainFile()
  - GenerateSceneTransitionPostamble()
  - GenerateSceneTransitionPreamble()
  - GenerateSystemFiles()
  - GetBuiltinModules()
  - GetColorsPerSlot()
  - GetFontConverter()
  - GetHardwarePalette()
  - GetHudStrategy()
  - GetMaxPalettesPerTilemap()
  - GetPaletteSlotCount()
  - GetPaletteSlotType()
  - GetRequiredToolchainBinaries()
  - GetSettingsDefinitions()
  - GetTemplates()
  - GetToolchain()
  - InjectWarnings()

### FILE: SmsTilemapPreprocessorExtension.cs

Types:
  - SmsTilemapPreprocessorExtension
  - ProcessedTileEntry

Methods: 5
  - ConvertToSmsNametableWord()
  - Execute()
  - FormatMapAsHex()
  - GetDefaultParameters()
  - GetInt()

### FILE: SmsTilePackerExtension.cs

Types:
  - SmsTilePackerExtension

Methods: 2
  - Execute()
  - GetDefaultParameters()

---

## PROJECT: Retruxel.Lib.ImageProcessing

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\ImageProcessing\Retruxel.Lib.ImageProcessing

Files: 11

### FILE: BdfParser.cs

Types:
  - BdfParser

Methods: 2
  - Parse()
  - ReverseBits()

### FILE: ColorMatching.cs

Types:
  - ColorMatching
  - DistanceMode
  - LabColor
  - RgbColor
  - FastColor

Methods: 8
  - BitmapFromByteArray()
  - ColorDistance()
  - FindNearestCentroid()
  - FindNearestColorIndex()
  - LabF()
  - OptimizePalette()
  - QuantizePalette()
  - RgbToLinear()

### FILE: CompressionStubs.cs

Error reading file

### FILE: IndexedBitmapRenderer.cs

Types:
  - is
  - IndexedBitmapRenderer

Methods: 3
  - ExtractPixels()
  - Render()
  - RenderTile()

### FILE: IndexedPngService.cs

Types:
  - IndexedPngService
  - IndexedPngData

Methods: 3
  - FindNearestIndex()
  - RenderPreview()
  - Write()

### FILE: NametableDecoder.cs

Types:
  - NametableDecoder

### FILE: PaletteExtractor.cs

Types:
  - PaletteExtractor

### FILE: PngReader.cs

Types:
  - PngReader

Methods: 1
  - LoadBitmap()

### FILE: TileConverter.cs

Types:
  - TileConverter

### FILE: TileFormat.cs

Types:
  - TileFormat
  - InterleaveMode

### FILE: TileSlicer.cs

Types:
  - TileSlicer

---

## PROJECT: Retruxel.Lib.PaletteHelpers

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Lib.PaletteHelpers

Files: 1

### FILE: PaletteHelpers.cs

Types:
  - PaletteHelpers

Methods: 6
  - FindClosestHardwareColor()
  - OpenPaletteEditorForSlot()
  - ParseSlotIndexFromComboBoxItem()
  - PopulatePaletteSlotComboBox()
  - ResolvePaletteColors()
  - SavePaletteSlotToModuleData()

---

## PROJECT: Retruxel.Lib.WPFImageProcessing

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Lib.WPFImageProcessing

Files: 1

### FILE: ImageProcessing.cs

Types:
  - ImageProcessing

Methods: 2
  - BitmapSourceBitmapToSkia()
  - ConvertSkBitmapToBitmapSource()

---

## PROJECT: Retruxel.Tool.AnimationPreprocessor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.AnimationPreprocessor

Files: 1

### FILE: AnimationPreprocessorTool.cs

Types:
  - AnimationPreprocessorTool
  - AnimationClip

Methods: 4
  - Execute()
  - GenerateEnumEntries()
  - GenerateFrameArrays()
  - Validate()

---

## PROJECT: Retruxel.Tool.AssetImporter

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.AssetImporter

Files: 4

### FILE: AssetImporter.cs

Types:
  - AssetImporter
  - AssetImportException

Methods: 4
  - AssetImportException()
  - Import()
  - ReduceColorsToHardware()
  - ValidateDimensions()

### FILE: AssetImporterWindow.xaml.cs

Types:
  - AssetImporterWindow

Methods: 24
  - ApplyLocalization()
  - BtnBrowse_Click()
  - BtnCancel_Click()
  - BtnCaptureEmulator_Click()
  - BtnClose_Click()
  - BtnImport_Click()
  - ClearValidation()
  - CountUniqueColors()
  - DropZone_DragOver()
  - DropZone_Drop()
  - GenerateReducedPreview()
  - GenerateRegionControls()
  - GetSelectedRegionId()
  - LoadSourceImage()
  - OnClosed()
  - PreSelectRegion()
  - ProcessEmulatorCapture()
  - RbSource_Changed()
  - ShowPaletteImportDialog()
  - ShowValidation()
  - TitleBar_MouseLeftButtonDown()
  - TxtAssetName_TextChanged()
  - UpdateImportButton()
  - ValidateAssetName()

### FILE: PaletteImportDialog.xaml.cs

Types:
  - PaletteImportDialog

Methods: 7
  - BtnCancel_Click()
  - BtnOk_Click()
  - BuildColorPreview()
  - BuildSlotOptions()
  - BuildTransparentColorSelector()
  - MakeSwatch()
  - UpdateTransparentColorSelection()

### FILE: PaletteImportResult.cs

Types:
  - PaletteImportResult

---

## PROJECT: Retruxel.Tool.AssetProcessor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.AssetProcessor

Files: 2

### FILE: AssetProcessorTool.cs

Types:
  - AssetProcessorTool

Methods: 3
  - ApplyGenerationParams()
  - Execute()
  - LoadNormalizedBitmap()

### FILE: PaletteOptimizationWindow.xaml.cs

Types:
  - PaletteOptimizationWindow

Methods: 12
  - ApplyPalette()
  - BtnCancel_Click()
  - BtnOk_Click()
  - ColorSpace_Changed()
  - ConfirmPalette()
  - RefreshPaletteSwatches()
  - RgbToHue()
  - RgbToLightness()
  - RgbToSaturation()
  - SliderDiversity_ValueChanged()
  - SortPalette()
  - UpdateOptimizedPreview()

---

## PROJECT: Retruxel.Tool.AudioEditor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.AudioEditor

Files: 1

### FILE: AudioEditorTool.cs

Types:
  - AudioEditorTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.AutoPorting

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.AutoPorting

Files: 1

### FILE: AutoPortingTool.cs

Types:
  - AutoPortingTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.BankManager

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.BankManager

Files: 1

### FILE: BankManagerTool.cs

Types:
  - BankManagerTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.CodeAnalyzer

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.CodeAnalyzer

Files: 1

### FILE: CodeAnalyzerTool.cs

Types:
  - CodeAnalyzerTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.Collaboration

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.Collaboration

Files: 1

### FILE: CollaborationTool.cs

Types:
  - CollaborationTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.CompressionEngine

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.CompressionEngine

Files: 1

### FILE: CompressionEngineTool.cs

Types:
  - CompressionEngineTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.ConstraintAnalyzer

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.ConstraintAnalyzer

Files: 1

### FILE: ConstraintAnalyzerTool.cs

Types:
  - ConstraintAnalyzerTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.CreationAssistant

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.CreationAssistant

Files: 1

### FILE: CreationAssistantTool.cs

Types:
  - CreationAssistantTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.FontImporter

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.FontImporter

Files: 3

### FILE: FontImporterWindow.xaml.cs

Types:
  - FontImporterWindow

Methods: 22
  - Antialiasing_Changed()
  - BrowseButton_Click()
  - BuildCell()
  - BuildCharGrid()
  - ClearSelection_Click()
  - CloseButton_Click()
  - FontSize_Changed()
  - ImportButton_Click()
  - Offset_Changed()
  - RefreshCellStyle()
  - RegenerateAllGlyphs()
  - SelectAll_Click()
  - SelectAsciiBasic_Click()
  - SelectAsciiBasicRange()
  - SizePreset_Click()
  - TileSize_Changed()
  - TitleBar_MouseLeftButtonDown()
  - ToggleCell()
  - UpdateGlyphPreview()
  - UpdateImportButton()
  - UpdateSheetPreview()
  - UpdateStats()

### FILE: FontImportResult.cs

Types:
  - FontImportResult

### FILE: FontRasterizer.cs

Types:
  - FontRasterizer

Methods: 4
  - BuildFont()
  - BuildPaint()
  - DrawGlyph()
  - RenderSpritesheet()

---

## PROJECT: Retruxel.Tool.HardwareSimulator

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.HardwareSimulator

Files: 1

### FILE: HardwareSimulatorTool.cs

Types:
  - HardwareSimulatorTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.HybridCodeEditor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.HybridCodeEditor

Files: 1

### FILE: HybridCodeEditorTool.cs

Types:
  - HybridCodeEditorTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.IntelligenceEngine

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.IntelligenceEngine

Files: 1

### FILE: IntelligenceEngineTool.cs

Types:
  - IntelligenceEngineTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.LiveLink

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.LiveLink

Files: 23

### FILE: BgbConnection.cs

Types:
  - BgbConnection

Methods: 7
  - ConnectAsync()
  - DisconnectAsync()
  - GetStateAsync()
  - ParseByteResponse()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SendCommandAsync()

### FILE: CanvasExpansionCapture.cs

Types:
  - CanvasExpansionCapture
  - ExpansionDirection
  - ExpansionResult
  - SearchResult

Methods: 4
  - EstimateMapHeight()
  - ExpandCanvas()
  - SearchFullNametableInRom()
  - SearchNametableInData()

### FILE: CanvasExpansionWindow.xaml.cs

Types:
  - CanvasExpansionWindow

Methods: 5
  - BtnCancel_Click()
  - BtnClose_Click()
  - BtnExpand_Click()
  - TitleBar_MouseLeftButtonDown()
  - UpdatePreview()

### FILE: CaptureResult.cs

Types:
  - CaptureResult

### FILE: CaptureToImportedAssetPipeline.cs

Types:
  - CaptureToImportedAssetPipeline

Methods: 1
  - ProcessTyped()

### FILE: EmuliciousConnection.cs

Types:
  - EmuliciousConnection

Methods: 6
  - ConnectAsync()
  - DisconnectAsync()
  - GetStateAsync()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SendCommandAsync()

### FILE: LiveLinkTool.cs

Types:
  - LiveLinkTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: LiveLinkWindow.xaml.cs

Types:
  - file
  - files
  - LiveLinkWindow

### FILE: LiveLinkWindow_Capture.cs

Types:
  - LiveLinkWindow
  - paletteData

Methods: 16
  - BtnCaptureScreen_Click()
  - BtnCaptureVRAM_Click()
  - CaptureGameBoyPalette()
  - CaptureGameBoyTiles()
  - CaptureGameGearPalette()
  - CaptureNesNametable()
  - CaptureNesPalette()
  - CaptureNesTiles()
  - CaptureSg1000Nametable()
  - CaptureSg1000Palette()
  - CaptureSg1000Tiles()
  - CaptureSmsNametable()
  - CaptureSmsPalette()
  - CaptureSmsTiles()
  - CaptureSnesPalette()
  - CaptureSnesTiles()

### FILE: LiveLinkWindow_Connection.cs

Types:
  - LiveLinkWindow

Methods: 6
  - BtnConnect_Click()
  - DetectConsoleFromRom()
  - DiscoverEmulators()
  - GetDebugApiInstructions()
  - KeepAliveCheck()
  - StartKeepAlive()

### FILE: LiveLinkWindow_ExpandCanvas.cs

Types:
  - LiveLinkWindow

Methods: 1
  - BtnExpandCanvas_Click()

### FILE: LiveLinkWindow_Import.cs

Types:
  - LiveLinkWindow

Methods: 4
  - ApplyOptimizedPaletteToCapture()
  - BtnImport_Click()
  - ConvertBitmapToCapture()
  - ExtractPixelsFromBitmap()

### FILE: LiveLinkWindow_Main.cs

Types:
  - with
  - LiveLinkWindow

Methods: 1
  - OnWindowClosing()

### FILE: LiveLinkWindow_Palette.cs

Types:
  - LiveLinkWindow

Methods: 1
  - BtnValidateSmsColors_Click()

### FILE: LiveLinkWindow_Preview.cs

Types:
  - LiveLinkWindow

Methods: 2
  - RenderPreview()
  - RenderTilesInGrid()

### FILE: LiveLinkWindow_Specs.cs

Types:
  - LiveLinkWindow

### FILE: LiveLinkWindow_UI.cs

Types:
  - LiveLinkWindow

Methods: 9
  - AppendLog()
  - BtnClearLog_Click()
  - BtnClose_Click()
  - BtnCopyLog_Click()
  - LogError()
  - LogInfo()
  - LogSuccess()
  - LogWarning()
  - TitleBar_MouseLeftButtonDown()

### FILE: MesenConnection.cs

Types:
  - MesenConnection

Methods: 10
  - ConnectAsync()
  - DisconnectAsync()
  - GetScreenBufferAsync()
  - GetStateAsync()
  - Log()
  - PingAsync()
  - ReadCramAsync()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SetLogCallback()

### FILE: MesenSConnection.cs

Types:
  - MesenSConnection

Methods: 6
  - ConnectAsync()
  - DisconnectAsync()
  - GetStateAsync()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SendCommandAsync()

### FILE: MgbaConnection.cs

Types:
  - MgbaConnection

Methods: 6
  - ConnectAsync()
  - DisconnectAsync()
  - GetStateAsync()
  - ReadMemoryAsync()
  - ReadVramAsync()
  - SendGdbCommandAsync()

### FILE: ScreenToTilesConverter.cs

Types:
  - ScreenToTilesConverter
  - ConversionResult
  - OptimizedPalette

Methods: 1
  - Convert()

### FILE: ScriptExtractor.cs

Types:
  - ScriptExtractor

Methods: 2
  - ExtractScript()
  - GetScriptPath()

### FILE: SmsColorValidator.cs

Types:
  - SmsColorValidator

Methods: 2
  - ExtractPixels()
  - GenerateSmsMasterPalette()

---

## PROJECT: Retruxel.Tool.MarketingKit

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.MarketingKit

Files: 1

### FILE: MarketingKitTool.cs

Types:
  - MarketingKitTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.MetaspritePreprocessor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.MetaspritePreprocessor

Files: 1

### FILE: MetaspritePreprocessorTool.cs

Types:
  - MetaspritePreprocessorTool
  - TileEntry

Methods: 3
  - Execute()
  - FormatIntArray()
  - GetInt()

---

## PROJECT: Retruxel.Tool.PaletteEditor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.PaletteEditor

Files: 2

### FILE: PaletteEditorTool.cs

Types:
  - PaletteEditorTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: PaletteEditorWindow.xaml.cs

Types:
  - PaletteEditorWindow
  - TargetPaletteProvider

Methods: 16
  - AddUsageText()
  - Apply()
  - ApplyLocalization()
  - BuildHardwareColorGrid()
  - BuildSlotsGrid()
  - CreateNewPalette()
  - DeletePalette()
  - DuplicatePalette()
  - GetColorFormat()
  - InitializeUI()
  - LoadDefaultPalette()
  - OnClosed()
  - RefreshSlots()
  - SelectSlot()
  - SetSlotColor()
  - UpdateUsageReport()

---

## PROJECT: Retruxel.Tool.PixelArtEditor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.PixelArtEditor

Files: 1

### FILE: PixelArtEditorTool.cs

Types:
  - PixelArtEditorTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.PlaytestBot

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.PlaytestBot

Files: 1

### FILE: PlaytestBotTool.cs

Types:
  - PlaytestBotTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.PngToTiles

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.PngToTiles

Files: 1

### FILE: PngToTilesTool.cs

Types:
  - PngToTilesTool

Methods: 3
  - Execute()
  - GetDefaultParameters()
  - GetInt()

---

## PROJECT: Retruxel.Tool.SpriteEditor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.SpriteEditor

Files: 20

### FILE: HitboxDefinition.cs

Types:
  - HitboxDefinition
  - HitboxType

### FILE: HitboxTypeDialog.xaml.cs

Types:
  - HitboxTypeDialog

Methods: 2
  - BtnAdd_Click()
  - BtnCancel_Click()

### FILE: LiveLinkSpriteCaptureDialog.xaml.cs

Types:
  - LiveLinkSpriteCaptureDialog

Methods: 4
  - BtnCancel_Click()
  - BtnCapture_Click()
  - TxtTileCount_TextChanged()
  - UpdateVramSize()

### FILE: SpriteEditorTool.cs

Types:
  - SpriteEditorTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: SpriteEditorWindow.xaml.cs

Types:
  - file
  - files
  - SpriteEditorWindow

### FILE: SpriteEditorWindow_Animation.cs

Types:
  - SpriteEditorWindow

Methods: 7
  - AnimationTimer_Tick()
  - BtnPlayPause_Click()
  - InitializeAnimation()
  - RenderPreview()
  - StartAnimation()
  - StopAnimation()
  - UpdateAnimationSpeed()

### FILE: SpriteEditorWindow_Asset.cs

Types:
  - SpriteEditorWindow

Methods: 5
  - BtnBrowseTileset_Click()
  - CmbTilesetAsset_SelectionChanged()
  - InitializeAssetSelector()
  - LoadTilesetFromAsset()
  - SaveAssetSelection()

### FILE: SpriteEditorWindow_Canvas.cs

Types:
  - SpriteEditorWindow

Methods: 10
  - AddTileToCurrentFrame()
  - Canvas_DragOver()
  - Canvas_Drop()
  - CanvasTile_MouseDown()
  - CanvasTile_MouseMove()
  - CanvasTile_MouseUp()
  - DrawGrid()
  - RemoveTileFromCurrentFrame()
  - RenderCanvas()
  - UpdateStatusBar()

### FILE: SpriteEditorWindow_Frames.cs

Types:
  - SpriteEditorWindow

Methods: 7
  - BtnAddFrame_Click()
  - BtnDeleteFrame_Click()
  - BtnDuplicateFrame_Click()
  - FramesListBox_SelectionChanged()
  - RefreshFramesList()
  - TxtFrameDuration_TextChanged()
  - UpdateFrameDurationField()

### FILE: SpriteEditorWindow_Hitboxes.cs

Types:
  - SpriteEditorWindow

Methods: 4
  - BtnAddHitbox_Click()
  - BtnDeleteHitbox_Click()
  - DrawHitboxes()
  - RefreshHitboxList()

### FILE: SpriteEditorWindow_Initialization.cs

Types:
  - SpriteEditorWindow

Methods: 7
  - BtnCancel_Click()
  - BtnClose_Click()
  - BtnSave_Click()
  - InitializeUI()
  - LoadModuleData()
  - SaveModuleData()
  - TitleBar_MouseLeftButtonDown()

### FILE: SpriteEditorWindow_LiveLink.cs

Types:
  - SpriteEditorWindow

Methods: 1
  - BtnLiveLink_Click()

### FILE: SpriteEditorWindow_Main.cs

Types:
  - SpriteEditorWindow

Methods: 1
  - OnSpriteChanged()

### FILE: SpriteEditorWindow_PaletteSlot.cs

Types:
  - SpriteEditorWindow

Methods: 4
  - BtnEditPalette_Click()
  - CmbPaletteSlot_SelectionChanged()
  - InitializePaletteSlotSelector()
  - SavePaletteSlotSelection()

### FILE: SpriteEditorWindow_Tileset.cs

Types:
  - SpriteEditorWindow

Methods: 8
  - CreateTileButton()
  - ExtractTile()
  - RefreshTilesetWithPalette()
  - RenderTileset()
  - TileButton_Click()
  - TileButton_MouseMove()
  - UpdateTilesetSelection()
  - UpdateVramInfo()

### FILE: SpriteEditorWindow_Zoom.cs

Types:
  - SpriteEditorWindow

Methods: 7
  - GetCanvasDisplaySize()
  - GetCanvasZoom()
  - GetTileDisplaySize()
  - InitializeZoomControls()
  - RefreshTilesetFromAsset()
  - SetCanvasZoom()
  - SetTilesetZoom()

### FILE: SpriteFrame.cs

Types:
  - SpriteFrame

Methods: 1
  - Clone()

### FILE: SpriteRenderer.cs

Types:
  - SpriteRenderer

Methods: 3
  - CreateEmptyBitmap()
  - ExtractTile()
  - RenderFrame()

### FILE: SpriteState.cs

Types:
  - SpriteState

### FILE: SpriteTile.cs

Types:
  - SpriteTile

Methods: 1
  - Clone()

---

## PROJECT: Retruxel.Tool.TextArrayEditor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.TextArrayEditor

Files: 9

### FILE: TextArrayEditorTool.cs

Types:
  - TextArrayEditorTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: TextArrayEditorWindow.xaml.cs

Types:
  - file
  - files
  - TextArrayEditorWindow

### FILE: TextArrayEditorWindow_ArrayName.cs

Types:
  - TextArrayEditorWindow

Methods: 2
  - IsValidIdentifier()
  - TxtArrayNameInput_TextChanged()

### FILE: TextArrayEditorWindow_Font.cs

Types:
  - TextArrayEditorWindow
  - FontCategoryItem
  - AsciiMapItem

Methods: 10
  - BtnBrowseFont_Click()
  - BtnImportFromTtf_Click()
  - BtnLiveLink_Click()
  - CmbFontCategory_SelectionChanged()
  - DrawGridOverlay()
  - ExpandMoreFontsAsync()
  - PopulateAsciiMap()
  - PopulateFontCategories()
  - RenderFontPreview()
  - RenderRepositoryFontPreviewAsync()

### FILE: TextArrayEditorWindow_Languages.cs

Types:
  - TextArrayEditorWindow

Methods: 3
  - BtnAddLanguage_Click()
  - RefreshLanguageTabs()
  - SelectLanguage()

### FILE: TextArrayEditorWindow_Main.cs

Types:
  - with
  - TextArrayEditorWindow
  - TextArrayState
  - TextLanguage
  - TextInputDialog

### FILE: TextArrayEditorWindow_Preview.cs

Types:
  - TextArrayEditorWindow

Methods: 1
  - RenderPreview()

### FILE: TextArrayEditorWindow_Strings.cs

Types:
  - TextArrayEditorWindow

Methods: 3
  - BtnAddString_Click()
  - RefreshStringsList()
  - RemoveStringAtIndex()

### FILE: TextArrayEditorWindow_Window.cs

Types:
  - TextArrayEditorWindow

Methods: 8
  - ActivateTab()
  - BtnCancel_Click()
  - BtnClose_Click()
  - BtnSave_Click()
  - BtnTabFont_Click()
  - BtnTabStrings_Click()
  - SaveAndClose()
  - TitleBar_MouseLeftButtonDown()

---

## PROJECT: Retruxel.Tool.TilemapEditor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.TilemapEditor

Files: 21

### FILE: ImportedAssetToTilemapPipeline.cs

Types:
  - ImportedAssetToTilemapPipeline

Methods: 12
  - CalculateTilesetHeight()
  - CalculateTilesetWidth()
  - CalculateTilesPerRow()
  - DrawTileToBitmap()
  - FindClosestHardwareColor()
  - GenerateUniqueAssetId()
  - LabF()
  - ProcessTyped()
  - SaveBitmapDirectly()
  - SaveTilesAsAsset()
  - SaveTilesReconstructed()
  - SrgbToLinear()

### FILE: PaletteMapper.cs

Types:
  - PaletteMapper

Methods: 3
  - FindClosestHardwareColor()
  - LabF()
  - RgbToLinear()

### FILE: TilemapData.cs

Types:
  - TilemapData

Methods: 9
  - ClearLayer()
  - FillLayer()
  - GetTile()
  - GetTileIndex()
  - Initialize()
  - Resize()
  - SetTile()

### FILE: TilemapEditorTool.cs

Types:
  - TilemapEditorTool

Methods: 2
  - CreateWindow()
  - Execute()

### FILE: TilemapEditorWindow.xaml.cs

Types:
  - ToolMode
  - TilemapEditorWindow

### FILE: TilemapEditorWindow_Actions.cs

Types:
  - TilemapEditorWindow

Methods: 9
  - BtnCancel_Click()
  - BtnClear_Click()
  - BtnClose_Click()
  - BtnFill_Click()
  - BtnImportTilesetAsMap_Click()
  - BtnSave_Click()
  - LoadFromBase64()
  - LoadModuleData()
  - TitleBar_MouseLeftButtonDown()

### FILE: TilemapEditorWindow_Assets.cs

Types:
  - TilemapEditorWindow

Methods: 2
  - BtnImportAsset_Click()
  - BtnImportFromLiveLink_Click()

### FILE: TilemapEditorWindow_Canvas.cs

Types:
  - TilemapEditorWindow

Methods: 12
  - Canvas_MouseDown()
  - Canvas_MouseLeave()
  - Canvas_MouseLeftButtonDown()
  - Canvas_MouseLeftButtonUp()
  - Canvas_MouseMove()
  - Canvas_MouseRightButtonDown()
  - Canvas_MouseUp()
  - Canvas_MouseWheel()
  - DrawViewportOverlay()
  - PaintTile()
  - RenderCanvas()
  - RenderTileAt()

### FILE: TilemapEditorWindow_Flip.cs

Types:
  - TilemapEditorWindow

Methods: 2
  - InitializeFlipHotkeys()
  - Window_KeyDown()

### FILE: TilemapEditorWindow_Initialization.cs

Types:
  - TilemapEditorWindow

Methods: 1
  - InitializeUI()

### FILE: TilemapEditorWindow_Layers.cs

Types:
  - TilemapEditorWindow

Methods: 2
  - ChkShowCollision_CheckedChanged()
  - CmbLayers_SelectionChanged()

### FILE: TilemapEditorWindow_Offset.cs

Types:
  - TilemapEditorWindow

Methods: 5
  - BtnOffsetDown_Click()
  - BtnOffsetLeft_Click()
  - BtnOffsetRight_Click()
  - BtnOffsetUp_Click()
  - UpdateMapOffset()

### FILE: TilemapEditorWindow_PaletteOptimization.cs

Types:
  - TilemapEditorWindow

Methods: 3
  - OpenColorOptimizationForAsset()
  - ReduceColorsToHardware()
  - SaveSkBitmapToFile()

### FILE: TilemapEditorWindow_PaletteSlot.cs

Types:
  - TilemapEditorWindow

Methods: 6
  - BtnEditPalette_Click()
  - CmbPalette_DropDownClosed()
  - CmbPalette_SelectionChanged()
  - InitializePaletteSlotSelector()
  - OpenPaletteEditor()
  - SavePaletteSlotSelection()

### FILE: TilemapEditorWindow_Selection.cs

Types:
  - TilemapEditorWindow

Methods: 12
  - GetTilePosition()
  - HidePaintPreview()
  - InitializeSelection()
  - PlaceTileBlock()
  - RenderTileBlock()
  - ShowPaintPreview()
  - TileButton_MouseDown()
  - TileButton_MouseMove()
  - TileButton_MouseUp()
  - UpdateRectangularSelection()
  - UpdateSelectedTilePreview()
  - UpdateTileSelectionVisual()

### FILE: TilemapEditorWindow_Tileset.cs

Types:
  - TilemapEditorWindow

Methods: 20
  - CmbTilesetAsset_SelectionChanged()
  - DrawTilesetGrid()
  - FindClosestHardwareColor()
  - HitTestTile()
  - LoadAssets()
  - LoadTilesetImage()
  - RebuildTilesetBitmap()
  - RefreshTilesetFromAsset()
  - RenderTilesetCanvas()
  - ResolvePaletteColors()
  - SetTilesetGridColor()
  - TilesetCanvas_MouseLeftButtonDown()
  - TilesetCanvas_MouseLeftButtonUp()
  - TilesetCanvas_MouseMiddleButtonDown()
  - TilesetCanvas_MouseMiddleButtonUp()
  - TilesetCanvas_MouseMove()
  - TilesetCanvas_MouseWheel()
  - TilesetScrollViewer_PreviewMouseWheel()
  - ToggleTilesetGrid()
  - UpdateTileselectionOverlay()

### FILE: TilemapEditorWindow_Tools.cs

Types:
  - TilemapEditorWindow

Methods: 5
  - ApplyOptimization()
  - BtnExportPng_Click()
  - BtnOptimize_Click()
  - CreateOptimizedTileset()
  - UpdateAssetWithOptimization()

### FILE: TilemapEditorWindow_Zoom.cs

Types:
  - TilemapEditorWindow

Methods: 21
  - AddMenuItem()
  - BtnCanvasZoom_Click()
  - BtnTilesetZoom_Click()
  - HandleCanvasMouseWheel()
  - HandleCanvasPanEnd()
  - HandleCanvasPanMove()
  - HandleCanvasPanStart()
  - HandleTilesetMouseWheel()
  - HandleTilesetPanEnd()
  - HandleTilesetPanMove()
  - HandleTilesetPanStart()
  - OnKeyDown()
  - ScrollToOrigin()
  - SetZoom()
  - ShowZoomMenu()
  - StepZoom()
  - UpdateCanvasZoomLabel()
  - UpdateTilesetZoomLabel()
  - ZoomCanvasAtPoint()
  - ZoomTilesetAtPoint()
  - ZoomToFit()

### FILE: TilemapSerializer.cs

Types:
  - TilemapSerializer

Methods: 1
  - ToBase64()

### FILE: TilesetLayoutCalculator.cs

Types:
  - TilesetLayoutCalculator

Methods: 3
  - CalculateTilesetHeight()
  - CalculateTilesetWidth()
  - CalculateTilesPerRow()

### FILE: TilesetRenderer.cs

Types:
  - TilesetRenderer

Methods: 3
  - ApplyTransform()
  - LoadFromBitmap()
  - LoadTileset()

---

## PROJECT: Retruxel.Tool.TilemapPreprocessor

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.TilemapPreprocessor

Files: 3

### FILE: ProcessedTileEntry.cs

Types:
  - ProcessedTileEntry

### FILE: TileEntry.cs

Types:
  - TileEntry

### FILE: TilemapPreprocessorTool.cs

Types:
  - TilemapPreprocessorTool

Methods: 3
  - Execute()
  - GetInt()
  - Validate()

---

## PROJECT: Retruxel.Tool.TilePacker

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.TilePacker

Files: 1

### FILE: TilePackerTool.cs

Types:
  - TilePackerTool

Methods: 3
  - ComputeHash()
  - Execute()
  - PackTiles()

---

## PROJECT: Retruxel.Tool.VisualScripting

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.VisualScripting

Files: 1

### FILE: VisualScriptingTool.cs

Types:
  - VisualScriptingTool

Methods: 1
  - Execute()

---

## PROJECT: Retruxel.Tool.Wizard

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Plugins\Tools\Retruxel.Tool.Wizard

Files: 22

### FILE: CodeGenGenerator.cs

Types:
  - CodeGenGenerator

Methods: 4
  - Generate()
  - GenerateCodeGenJson()
  - GenerateCodeTemplate()
  - GenerateReadme()

### FILE: CodeGenStep1BasicInfo.xaml.cs

Types:
  - CodeGenStep1BasicInfo

Methods: 4
  - AttachHandlers()
  - InstallTarget_Click()
  - LoadData()
  - LoadTargets()

### FILE: CodeGenStep2Code.xaml.cs

Types:
  - CodeGenStep2Code

Methods: 4
  - AttachHandlers()
  - Browse_Click()
  - LoadData()
  - SourceComboBox_SelectionChanged()

### FILE: CodeGenStep3VariableMapping.xaml.cs

Types:
  - CodeGenStep3VariableMapping
  - CodeGenVariableMappingItem

Methods: 4
  - DetectVariables()
  - GetDefaultJsonPath()
  - GetDefaultType()
  - OnPropertyChanged()

### FILE: CodeGenStep4Dependencies.xaml.cs

Types:
  - CodeGenStep4Dependencies

Methods: 2
  - LoadTools()
  - UpdateSelectedToolsDisplay()

### FILE: CodeGenWizardWindow.xaml.cs

Types:
  - CodeGenWizardWindow

Methods: 8
  - Back_Click()
  - Cancel_Click()
  - GenerateCodeGen()
  - InitializeSteps()
  - Next_Click()
  - ShowStep()
  - TitleBar_MouseLeftButtonDown()
  - ValidateCurrentStep()

### FILE: TargetGenerator.cs

Types:
  - TargetGenerator

Methods: 7
  - Generate()
  - GenerateCsproj()
  - GenerateMainTemplate()
  - GenerateReadme()
  - GenerateTargetClass()
  - GenerateTargetJson()
  - GenerateToolchainAdapter()

### FILE: TargetStep1BasicInfo.xaml.cs

Types:
  - TargetStep1BasicInfo

Methods: 2
  - AttachHandlers()
  - LoadData()

### FILE: TargetStep2Specs.xaml.cs

Types:
  - TargetStep2Specs

Methods: 2
  - AttachHandlers()
  - LoadData()

### FILE: TargetStep3Toolchain.xaml.cs

Types:
  - TargetStep3Toolchain

Methods: 2
  - AttachHandlers()
  - LoadData()

### FILE: TargetStep4MainFile.xaml.cs

Types:
  - TargetStep4MainFile

Methods: 5
  - AttachHandlers()
  - Browse_Click()
  - GenerateDefaultTemplate()
  - LoadData()
  - SourceComboBox_SelectionChanged()

### FILE: TargetStep5VariableMapping.xaml.cs

Types:
  - TargetStep5VariableMapping
  - VariableMappingItem

Methods: 3
  - DetectVariables()
  - GetDefaultMapping()
  - OnPropertyChanged()

### FILE: TargetWizardWindow.xaml.cs

Types:
  - TargetWizardWindow

Methods: 8
  - Back_Click()
  - Cancel_Click()
  - GenerateTarget()
  - InitializeSteps()
  - Next_Click()
  - ShowStep()
  - TitleBar_MouseLeftButtonDown()
  - ValidateCurrentStep()

### FILE: TemplateLoader.cs

Types:
  - TemplateLoader

Methods: 1
  - LoadTemplate()

### FILE: ToolGenerator.cs

Types:
  - ToolGenerator

Methods: 6
  - Execute()
  - Generate()
  - GenerateCsproj()
  - GenerateReadme()
  - GenerateToolClass()
  - Validate()

### FILE: ToolStep1BasicInfo.xaml.cs

Types:
  - ToolStep1BasicInfo

Methods: 2
  - AttachHandlers()
  - LoadData()

### FILE: ToolStep2Configuration.xaml.cs

Types:
  - ToolStep2Configuration

Methods: 3
  - AttachHandlers()
  - LoadData()
  - LoadTargets()

### FILE: ToolStep3Parameters.xaml.cs

Types:
  - ToolStep3Parameters
  - ToolParameterItem

Methods: 5
  - AddParameter_Click()
  - LoadParameters()
  - OnParameterChanged()
  - OnPropertyChanged()
  - RemoveParameter_Click()

### FILE: ToolWizardWindow.xaml.cs

Types:
  - ToolWizardWindow

Methods: 8
  - Back_Click()
  - Cancel_Click()
  - GenerateTool()
  - InitializeSteps()
  - Next_Click()
  - ShowStep()
  - TitleBar_MouseLeftButtonDown()
  - ValidateCurrentStep()

### FILE: WizardData.cs

Types:
  - TargetWizardData
  - CodeGenWizardData
  - VariableMapping
  - ToolWizardData
  - ToolParameter

### FILE: WizardMainWindow.xaml.cs

Types:
  - WizardMainWindow

Methods: 5
  - Close_Click()
  - CreateCodeGen_Click()
  - CreateTarget_Click()
  - CreateTool_Click()
  - TitleBar_MouseLeftButtonDown()

### FILE: WizardTool.cs

Types:
  - WizardTool

Methods: 2
  - Execute()
  - Validate()

---

## PROJECT: Retruxel.Core

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Retruxel.Core

Files: 106

### FILE: AppSettings.cs

Types:
  - AppSettings
  - GeneralSettings
  - WindowSettings
  - AppearanceSettings
  - TargetSettingsMap
  - TargetSettings

Methods: 1
  - ContainsKey()

### FILE: ArrayConversionHelper.cs

Types:
  - ArrayConversionHelper

### FILE: AssetEntry.cs

Types:
  - AssetEntry
  - AssetGenerationParams

### FILE: AssetPipelineBase.cs

Types:
  - to
  - AssetPipelineBase

Methods: 4
  - CanAccept()
  - CanProduce()
  - Process()
  - ProcessTyped()

### FILE: AssetToFileConnector.cs

Types:
  - AssetToFileConnector

Methods: 1
  - Connect()

### FILE: AssetToProjectConnector.cs

Types:
  - AssetToProjectConnector

Methods: 1
  - Connect()

### FILE: BasicLatin.cs

Types:
  - BasicLatin

Methods: 1
  - readonly()

### FILE: BlockElements.cs

Types:
  - BlockElements

Methods: 1
  - readonly()

### FILE: BoxDrawing.cs

Types:
  - BoxDrawing

Methods: 1
  - readonly()

### FILE: BuildContext.cs

Types:
  - BuildContext

### FILE: BuildDiagnostic.cs

Types:
  - DiagnosticSeverity
  - BuildDiagnosticMetric
  - Builder
  - BuildDiagnosticsReport

Methods: 9
  - Build()
  - WithCategory()
  - WithCurrent()
  - WithDetail()
  - WithDisplayName()
  - WithErrorThreshold()
  - WithMax()
  - WithMetricId()
  - WithWarningThreshold()

### FILE: BuildDiagnosticInput.cs

Types:
  - BuildDiagnosticInput

### FILE: BuildResult.cs

Types:
  - BuildResult
  - BuildLogEntry
  - BuildLogLevel

### FILE: CodeGenDiscovery.cs

Types:
  - CodeGenDiscovery
  - CodeGenManifestRaw

Methods: 3
  - DiscoverCodeGens()
  - Key()
  - ParseVariables()

### FILE: CodeGenerator.cs

Types:
  - CodeGenerator

Methods: 1
  - GenerateAsync()

### FILE: CodeGenerator_Batch.cs

Types:
  - CodeGenerator
  - the

Methods: 3
  - CalculateGraphicTilesEnd()
  - GenerateGameVarsFile()
  - GenerateTextArrayFile()

### FILE: CodeGenerator_Helpers.cs

Types:
  - CodeGenerator

Methods: 1
  - FormatByteArray()

### FILE: CodeGenerator_Modules.cs

Types:
  - CodeGenerator
  - ContextualModule

Methods: 5
  - Deserialize()
  - GetValidationSample()
  - InjectContextFlags()
  - InjectFlags()
  - Serialize()

### FILE: CodeGenerator_Validation.cs

Types:
  - CodeGenerator

Methods: 1
  - ValidateTileConflicts()

### FILE: CodeGenModels.cs

Types:
  - CodeGenManifest
  - VariableDefinition

### FILE: ConnectorRegistry.cs

Types:
  - ConnectorRegistry

Methods: 4
  - GetAll()
  - GetDefaults()
  - Register()
  - RegisterDefault()

### FILE: ConsoleDatabaseService.cs

Types:
  - ConsoleDatabaseService
  - ConsoleDatabase

Methods: 3
  - GetAllConsoleNames()
  - Load()
  - SearchConsoles()

### FILE: ConsoleSpec.cs

Types:
  - ConsoleSpec

### FILE: DefaultFont.cs

Types:
  - DefaultFont

Methods: 10
  - BuildGlyphs()
  - GetBasicLatinCharacters()
  - GetBlockElementsCharacters()
  - GetBoxDrawingCharacters()
  - GetExtendedLatinCharacters()
  - GetGreekCharacters()
  - GetHiraganaCharacters()
  - GetMiscellaneousCharacters()
  - GetSGACharacters()
  - Supports()

### FILE: EventCallGenerator.cs

Types:
  - EventCallGenerator

Methods: 3
  - GenerateEventCalls()
  - GenerateInputCalls()
  - GenerateUpdateCalls()

### FILE: ExecutionContext.cs

Types:
  - ExecutionContext

### FILE: ExtendedLatin.cs

Types:
  - ExtendedLatin

Methods: 1
  - readonly()

### FILE: FileExportConnector.cs

Types:
  - FileExportConnector

Methods: 2
  - Connect()
  - ExportToFile()

### FILE: FileFilterProcessor.cs

Types:
  - FileFilterProcessor

Methods: 4
  - EvaluateFileFilter()
  - GetFileProperty()
  - ProcessModuleFiles()
  - TransformFile()

### FILE: FontRepository.cs

Types:
  - FontRepository
  - FontRepositoryItem

Methods: 1
  - GetRandomCdnUrl()

### FILE: GameState.cs

Types:
  - GameState
  - TilemapLayerState
  - SpriteState
  - SpriteLayerState
  - LayerType

Methods: 1
  - ClearDirtyFlags()

### FILE: GeneratedAsset.cs

Types:
  - GeneratedAsset
  - GeneratedAssetType

### FILE: GeneratedFile.cs

Types:
  - GeneratedFile
  - GeneratedFileType

### FILE: GlyphTile.cs

Types:
  - GlyphTile
  - TileSelectedEventArgs
  - TileDragEventArgs

### FILE: Greek.cs

Types:
  - Greek

Methods: 1
  - readonly()

### FILE: HardwareColor.cs

Methods: 3
  - FromHex()
  - HardwareColor()
  - ToHex()

### FILE: Hiragana.cs

Types:
  - Hiragana

Methods: 1
  - readonly()

### FILE: HudStrategy.cs

Types:
  - HudStrategy

### FILE: IAssetPipeline.cs

Types:
  - IAssetPipeline
  - IAssetPipeline

### FILE: IAudioModule.cs

Types:
  - for
  - IAudioModule

### FILE: ICodeGenPlugin.cs

Types:
  - ICodeGenPlugin

### FILE: IEmulatorConnection.cs

Types:
  - IEmulatorConnection
  - EmulatorState

### FILE: IFontConverter.cs

Types:
  - IFontConverter

### FILE: IGraphicModule.cs

Types:
  - IGraphicModule

### FILE: ILocalizationService.cs

Types:
  - ILocalizationService

### FILE: ILogicModule.cs

Types:
  - ILogicModule

### FILE: IModule.cs

Types:
  - IModule
  - it
  - ModuleType

### FILE: ImportedAssetData.cs

Types:
  - ImportedAssetData

Methods: 2
  - GetSummary()
  - IsValid()

### FILE: IPaletteConverter.cs

Types:
  - IPaletteConverter

### FILE: IPaletteProvider.cs

Types:
  - IPaletteProvider

### FILE: IRenderBackend.cs

Types:
  - to
  - IRenderBackend
  - RenderBackendConstraints

### FILE: ITarget.cs

Types:
  - ITarget

### FILE: ITool.cs

Types:
  - ITool

### FILE: IToolchain.cs

Types:
  - IToolchain

### FILE: IToolchainBuilder.cs

Types:
  - IToolchainBuilder

### FILE: IToolConnector.cs

Types:
  - IToolConnector

### FILE: IToolExtension.cs

Types:
  - IToolExtension

### FILE: IUndoableCommand.cs

Types:
  - IUndoableCommand

### FILE: IVisualTool.cs

Types:
  - IVisualTool

### FILE: LocalizationService.cs

Types:
  - LocalizationService

Methods: 6
  - DetectSystemLanguage()
  - DiscoverLanguages()
  - Get()
  - LanguageInfo()
  - Load()
  - Translate()

### FILE: Miscellaneous.cs

Types:
  - Miscellaneous

Methods: 1
  - readonly()

### FILE: ModuleFilterProcessor.cs

Types:
  - ModuleFilterProcessor

Methods: 4
  - EvaluateModuleFilter()
  - EvaluateModuleIdFilter()
  - ProcessProjectModules()
  - ProcessSceneModules()

### FILE: ModuleLoader.cs

Types:
  - ModuleLoader

Methods: 8
  - IsCompatible()
  - LoadCompatible()
  - LoadFromPath()
  - RegisterAudioModule()
  - RegisterBuiltinModules()
  - RegisterGraphicModule()
  - RegisterLogicModule()
  - RegisterModulesFromAssembly()

### FILE: ModuleManifest.cs

Types:
  - ModuleManifest
  - ParameterDefinition
  - ParameterType

### FILE: ModuleOverride.cs

Types:
  - ModuleOverride

### FILE: ModuleRegistry.cs

Types:
  - ModuleRegistry

Methods: 8
  - ApplyPolicyOverrides()
  - GetModulePolicy()
  - IsModuleSingleton()
  - LoadForTarget()
  - RegisterAudioModule()
  - RegisterBuiltinModules()
  - RegisterGraphicModule()
  - RegisterLogicModule()

### FILE: ModuleRenderer.cs

Types:
  - ModuleRenderer

Methods: 12
  - CanRender()
  - GetStandaloneTools()
  - GetUserModules()
  - Key()
  - Render()
  - RenderBatch()
  - RenderSceneFiles()
  - ResetState()
  - SanitizeFileName()
  - SetGlobalVariables()
  - SetModuleRegistry()
  - SetTargetAssembly()

### FILE: ModuleScope.cs

Types:
  - ModuleScope

### FILE: PaletteSlotData.cs

Types:
  - PaletteSlotData

### FILE: PaletteSlotType.cs

Types:
  - PaletteSlotType

### FILE: PaletteToModuleConnector.cs

Types:
  - PaletteToModuleConnector

Methods: 1
  - Connect()

### FILE: PaletteToTilemapConnector.cs

Types:
  - PaletteToTilemapConnector

Methods: 1
  - Connect()

### FILE: PipelineRegistry.cs

Types:
  - PipelineRegistry

Methods: 6
  - DiscoverPipelines()
  - ExecuteChain()
  - GetAllPipelines()
  - GetPipelinesForInput()
  - GetPipelinesForOutput()
  - Register()

### FILE: ProjectManager.cs

Types:
  - ProjectManager

Methods: 6
  - ClearDirtyFlag()
  - Close()
  - CreateProject()
  - LoadAsync()
  - MarkDirty()
  - SaveAsync()

### FILE: ProjectTemplate.cs

Types:
  - ProjectTemplate

### FILE: ReflectionCodeGenHelper.cs

Types:
  - ReflectionCodeGenHelper
  - name

Methods: 3
  - BuildCodeGenCache()
  - ConvertPascalCaseToDotCase()
  - GenerateCodeForModule()

### FILE: RenderCommandBuffer.cs

Types:
  - RenderCommandType
  - RenderCommand
  - RenderCommandBuffer
  - DrawTilemapCommand
  - DrawTextCommand
  - DrawSpriteCommand
  - SetScrollCommand
  - LoadPaletteCommand

Methods: 2
  - AddCommand()
  - Clear()

### FILE: RetruxelProject.cs

Types:
  - RetruxelProject

### FILE: SceneData.cs

Types:
  - SceneData
  - SceneElementData

Methods: 1
  - GetEffectiveScope()

### FILE: SceneModuleConnector.cs

Types:
  - SceneModuleConnector

Methods: 1
  - Connect()

### FILE: ServiceLocator.cs

Types:
  - ServiceLocator

### FILE: SettingsService.cs

Types:
  - SettingsService

Methods: 3
  - GetTargetSettings()
  - Load()
  - Save()

### FILE: SGA.cs

Types:
  - SGA

Methods: 1
  - readonly()

### FILE: SingletonPolicy.cs

Types:
  - SingletonPolicy

### FILE: StartupService.cs

Types:
  - StartupService

### FILE: TargetModule.cs

Types:
  - TargetModule

Methods: 6
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: TargetPackageInfo.cs

Types:
  - TargetPackageInfo

### FILE: TargetPackageManager.cs

Types:
  - TargetPackageManager
  - TargetManifest

Methods: 6
  - ComputeSha256Async()
  - DownloadAndInstallAsync()
  - DownloadFileAsync()
  - ExtractPackageAsync()
  - IsInstalledAsync()
  - UninstallAsync()

### FILE: TargetRegistry.cs

Types:
  - TargetRegistry

Methods: 3
  - GetAllTargets()
  - GetManufacturers()
  - Initialize()

### FILE: TargetSpecs.cs

Types:
  - TargetSpecs
  - TilemapSpecs
  - PaletteMode

Methods: 2
  - RomBank()
  - VramRegion()

### FILE: TemplateEngine.cs

Types:
  - TemplateEngine

Methods: 7
  - CompareValues()
  - EvaluateCondition()
  - ExtractBlock()
  - LoadTemplate()
  - ProcessEachLoop()
  - Render()
  - RenderBlock()

### FILE: TextAnalyzer.cs

Types:
  - TextAnalyzer
  - TextAnalysisResult

Methods: 1
  - Analyze()

### FILE: TextAnalyzer.cs

Types:
  - TextAnalyzer
  - TextArrayState
  - TextLanguage
  - ComposedTileEntry

Methods: 3
  - Analyze()
  - FormatAsHexArray()
  - GenerateEmptyTranslationTable()

### FILE: TileEntry.cs

Types:
  - TileEntry

Methods: 1
  - Clone()

### FILE: TilePackerTool.cs

Types:
  - TilePackResult

### FILE: ToolchainManager.cs

Types:
  - ToolchainManager

Methods: 3
  - GetToolchain()
  - HasToolchain()
  - Register()

### FILE: ToolDiscovery.cs

Types:
  - ToolDiscovery

Methods: 1
  - DiscoverTools()

### FILE: ToolExecutionContext.cs

Types:
  - ToolExecutionContext

Methods: 2
  - AddError()
  - ChainResult()

### FILE: ToolExecutionResult.cs

Methods: 3
  - Error()
  - FromContext()
  - ToolExecutionResult()

### FILE: ToolExecutor.cs

Types:
  - ToolExecutor

Methods: 1
  - Execute()

### FILE: ToolFeedConnector.cs

Types:
  - ToolFeedConnector

Methods: 2
  - Connect()
  - MapOutput()

### FILE: ToolLoader.cs

Types:
  - ToolLoader

Methods: 8
  - DiscoverTools()
  - GetCategories()
  - GetTools()
  - GetToolsByCategory()
  - GetToolsForTarget()
  - LoadFromPath()
  - RegisterTool()
  - RegisterToolsFromAssembly()

### FILE: ToolRegistry.cs

Types:
  - ToolRegistry

Methods: 2
  - DiscoverTools()
  - RegisterTool()

### FILE: UndoableCommands.cs

Types:
  - AddElementCommand
  - RemoveElementCommand
  - MoveElementCommand
  - ChangePropertyCommand

Methods: 8
  - Execute()
  - Undo()

### FILE: UndoRedoStack.cs

Types:
  - UndoRedoStack

Methods: 5
  - Clear()
  - Push()
  - PushWithoutExecute()
  - Redo()
  - Undo()

### FILE: VariableResolver.cs

Types:
  - VariableResolver

Methods: 9
  - EvaluateComputedExpression()
  - InvokeTool()
  - ReadModuleValue()
  - ResolveAssetValue()
  - ResolveForModule()
  - ResolveSettingsValue()
  - SetCurrentScene()
  - SetGlobalVariables()
  - SetTargetAssembly()

---

## PROJECT: Retruxel.Emulation

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Retruxel.Emulation

Files: 4

### FILE: App.cs

Types:
  - App

### FILE: EmulatorWindow.xaml.cs

Types:
  - EmulatorWindow

Methods: 9
  - BtnDumpVram_Click()
  - BtnLoadCore_Click()
  - BtnLoadRom_Click()
  - BtnPause_Click()
  - BtnReset_Click()
  - BtnRun_Click()
  - EmulationLoop()
  - OnClosed()
  - OnVideoRefresh()

### FILE: LibRetroApi.cs

Types:
  - LibRetroApi
  - RetroPixelFormat
  - RetroDeviceType
  - RetroDeviceIdJoypad
  - RetroEnvironment
  - RetroMemory
  - RetroGameInfo
  - RetroSystemInfo
  - RetroSystemAvInfo
  - RetroGameGeometry
  - RetroSystemTiming

### FILE: LibRetroCore.cs

Types:
  - LibRetroCore

Methods: 13
  - AudioSampleBatchCallback()
  - AudioSampleCallback()
  - Dispose()
  - EnvironmentCallback()
  - InputPollCallback()
  - InputStateCallback()
  - LoadCore()
  - LoadCoreFunctions()
  - LoadGame()
  - Reset()
  - Run()
  - SetupCallbacks()
  - VideoRefreshCallback()

---

## PROJECT: Retruxel.Modules

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Retruxel.Modules

Files: 15

### FILE: AnimationModule.cs

Types:
  - AnimationModule
  - AnimationState
  - AnimationClip

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: EnemyModule.cs

Types:
  - EnemyModule
  - EnemyState

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: EntityModule.cs

Types:
  - EntityModule
  - EntityState

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: GameVarModule.cs

Types:
  - GameVarModule
  - GameVarState

Methods: 6
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: HudModule.cs

Types:
  - HudModule
  - HudState
  - HudElement
  - HudElementType

Methods: 6
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: InputModule.cs

Types:
  - InputModule
  - InputState

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: MetaspriteModule.cs

Types:
  - MetaspriteModule
  - MetaspriteState
  - MetaspriteTile

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: PaletteEffectModule.cs

Types:
  - PaletteEffectModule
  - PaletteEffectState

Methods: 6
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: PaletteModule.cs

Types:
  - PaletteModule
  - PaletteState

Methods: 6
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: PhysicsModule.cs

Types:
  - PhysicsModule
  - PhysicsState

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: ScrollModule.cs

Types:
  - ScrollModule
  - ScrollState

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: SpriteModule.cs

Types:
  - SpriteModule
  - SpriteState
  - SpriteTile

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: TextArrayModule.cs

Types:
  - TextArrayModule
  - TextArrayState
  - TextLanguage
  - ComposedTileEntry

Methods: 6
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: TextDisplayModule.cs

Types:
  - TextDisplayModule

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

### FILE: TilemapModule.cs

Types:
  - TilemapModule
  - TilemapState

Methods: 7
  - CreateEditorViewModel()
  - Deserialize()
  - GenerateAssets()
  - GenerateCode()
  - GetManifest()
  - GetValidationSample()
  - Serialize()

---

## PROJECT: Retruxel.SDK

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Retruxel.SDK

Files: 1

### FILE: RetruxelSdk.cs

---

## PROJECT: Retruxel.Toolchain

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Retruxel.Toolchain

Files: 7

### FILE: ColecoVisionToolchainBuilder.cs

Types:
  - ColecoVisionToolchainBuilder

Methods: 6
  - BuildAsync()
  - ComputeMd5Async()
  - ComputeSha256Async()
  - ExtractAsync()
  - RunProcessAsync()
  - VerifyAsync()

### FILE: GameGearToolchainBuilder.cs

Types:
  - GameGearToolchainBuilder

Methods: 6
  - BuildAsync()
  - ComputeMd5Async()
  - ComputeSha256Async()
  - ExtractAsync()
  - RunProcessAsync()
  - VerifyAsync()

### FILE: NesToolchainBuilder.cs

Types:
  - NesToolchainBuilder

Methods: 4
  - BuildAsync()
  - ExtractAsync()
  - RunProcessAsync()
  - VerifyAsync()

### FILE: Sg1000ToolchainBuilder.cs

Types:
  - Sg1000ToolchainBuilder

Methods: 6
  - BuildAsync()
  - ComputeMd5Async()
  - ComputeSha256Async()
  - ExtractAsync()
  - RunProcessAsync()
  - VerifyAsync()

### FILE: SmsToolchainBuilder.cs

Types:
  - SmsToolchainBuilder

Methods: 6
  - BuildAsync()
  - ComputeMd5Async()
  - ComputeSha256Async()
  - ExtractAsync()
  - RunProcessAsync()
  - VerifyAsync()

### FILE: ToolchainAdapter.cs

Types:
  - ToolchainAdapter

Methods: 3
  - BuildAsync()
  - ExtractAsync()
  - VerifyAsync()

### FILE: ToolchainOrchestrator.cs

Types:
  - ToolchainOrchestrator

Methods: 3
  - ClearCache()
  - EnsureBuildersDiscovered()
  - GetBuilder()

---

## PROJECT: Retruxel

Path: F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Retruxel

Files: 35

### FILE: AboutView.xaml.cs

Types:
  - AboutView
  - CreditEntry

Methods: 7
  - CreditLink_Click()
  - DeveloperLink_Click()
  - LoadCredits()
  - LoadLicenses()
  - OnLoaded()
  - OpenUrl()
  - ProjectLink_Click()

### FILE: App.xaml.cs

Types:
  - App

Methods: 1
  - App_Startup()

### FILE: AssemblyInfo.cs

### FILE: BuildConsoleView.xaml.cs

Types:
  - BuildConsoleView

Methods: 16
  - AppendLog()
  - BuildAsync()
  - CopyLog_Click()
  - CopyMd5_Click()
  - CopySha256_Click()
  - ExportDebug_Click()
  - ExportLog_Click()
  - ExportRom_Click()
  - LaunchEmulatorIfConfiguredAsync()
  - SetStatus()
  - ShowBuildStats()
  - ShowDiagnostics()
  - ShowMemoryStats()
  - ShowToast()
  - ShowVerification()
  - UpdateTooltips()

### FILE: BuildDiagnosticsPanel.xaml.cs

Types:
  - BuildDiagnosticsPanel

Methods: 2
  - CreateMetricPanel()
  - RebuildUI()

### FILE: MainWindow.xaml.cs

Types:
  - MainWindow

Methods: 24
  - AddToRecentProjects()
  - CloseButton_Click()
  - CloseOverlay_Click()
  - ConfigureWizardButton()
  - GetAppVersion()
  - HomeButton_Click()
  - MainWindow_Closing()
  - MainWindow_KeyDown()
  - MainWindow_Loaded()
  - MaximizeButton_Click()
  - MinimizeButton_Click()
  - OnGenerateRomRequested()
  - OnMouseMove()
  - OnMouseUp()
  - OnProjectCreated()
  - OverlayTitleBar_MouseLeftButtonDown()
  - RestoreWindowState()
  - SaveProjectAsync()
  - SaveWindowState()
  - SettingsButton_Click()
  - ShowOverlay()
  - TestWizard_Click()
  - TitleBar_MouseLeftButtonDown()
  - ToggleMaximize()

### FILE: ModuleParameterHelper.cs

Types:
  - ModuleParameterHelper

Methods: 2
  - SetValue()
  - UpdatePosition()

### FILE: NewProjectDialog.xaml.cs

Types:
  - NewProjectDialog

Methods: 7
  - BrowseButton_Click()
  - BuildTemplateCard()
  - CloseButton_Click()
  - CreateButton_Click()
  - LoadTemplates()
  - SelectTemplate()
  - TitleBar_MouseLeftButtonDown()

### FILE: SceneCanvasTransform.cs

Types:
  - SceneCanvasTransform

Methods: 4
  - CanvasToScreen()
  - Reset()
  - ScreenToCanvas()
  - ZoomAt()

### FILE: SceneEditorView.xaml.cs

Types:
  - SceneEditorView
  - SceneElement

Methods: 17
  - ApplyTargetSpecs()
  - BtnRedo_Click()
  - BtnTabAssets_Click()
  - BtnTabModules_Click()
  - BtnTabStructure_Click()
  - BtnUndo_Click()
  - Cleanup()
  - Documentation_Click()
  - GenerateRom_Click()
  - Initialize()
  - LoadFromProject()
  - OnSavingStateChanged()
  - SceneEditorView_KeyDown()
  - SetModuleRegistry()
  - SetProjectManager()
  - SyncProjectModules()
  - UpdateUndoRedoButtons()

### FILE: SceneEditorView_Assets.cs

Types:
  - handling
  - SceneEditorView

Methods: 8
  - BtnImportAsset_Click()
  - BtnImportSprites_Click()
  - BtnImportTiles_Click()
  - BuildAssetRow()
  - DeleteAsset()
  - DropAssetOnCanvas()
  - OpenAssetImporter()
  - RefreshAssetPanel()

### FILE: SceneEditorView_Canvas.cs

Types:
  - SceneEditorView

Methods: 7
  - AddModuleToCanvas()
  - BuildCanvasElement()
  - Canvas_DragOver()
  - Canvas_Drop()
  - Canvas_MouseLeftButtonDown()
  - Canvas_MouseMove()
  - GetModuleScope()

### FILE: SceneEditorView_Elements.cs

Types:
  - SceneEditorView

Methods: 11
  - AddElementFromData()
  - CreateSceneElement()
  - GetElementDisplayLabel()
  - OpenVisualToolForElement()
  - RefreshElementVisual()
  - RemoveElement()
  - RemoveElementCore()
  - SelectElement()
  - UpdateElementLabel()
  - UpdateElementPosition()
  - UpdateModulePosition()

### FILE: SceneEditorView_Events.cs

Types:
  - handling
  - SceneEditorView

Methods: 5
  - AddEventBlock()
  - AddModuleToEvent()
  - BuildEventAction()
  - LoadEvents()
  - ShowModulePicker()

### FILE: SceneEditorView_ModuleLabel.cs

Types:
  - SceneEditorView

Methods: 5
  - BuildModuleLabel()
  - GetCategoryColor()
  - GetModuleDisplayText()
  - GetModulePreview()
  - GetModuleTypeName()

### FILE: SceneEditorView_ModulePalette.cs

Types:
  - SceneEditorView

Methods: 3
  - BuildModulePaletteItem()
  - LoadModulePalette()
  - RefreshModulePalette()

### FILE: SceneEditorView_Properties.cs

Types:
  - handling
  - SceneEditorView

Methods: 5
  - AddParameterField()
  - AddScopeAndPolicyFields()
  - AddUserIdField()
  - BuildPropertiesPanel()
  - SetModuleParameterValue()

### FILE: SceneEditorView_Scenes.cs

Types:
  - handling
  - SceneEditorView

Methods: 8
  - ActivateScene()
  - BtnNewScene_Click()
  - BuildLabelForTab()
  - BuildSceneTab()
  - DeleteScene()
  - RebuildSceneTabs()
  - SetInitialScene()
  - StartInlineRename()

### FILE: SceneEditorView_Structure.cs

Types:
  - SceneEditorView

Methods: 12
  - AddStructureButton()
  - AddStructureHeader()
  - AddStructureSubheader()
  - BtnAddOnStartModule_Click()
  - BtnAddVariable_Click()
  - CreatePaletteSlotItem()
  - CreateStructureItem()
  - CreateStructureItemWithDelete()
  - OpenOnStartModuleEditor()
  - OpenPaletteSlotEditor()
  - ParseHexBrush()
  - RefreshStructurePanel()

### FILE: SceneEditorView_Visuals.cs

Types:
  - handling
  - SceneEditorView

### FILE: SceneElementFactory.cs

Types:
  - SceneElementFactory
  - SceneElement

Methods: 1
  - CreateElement()

### FILE: SettingsWindow.xaml.cs

Types:
  - file
  - files
  - SettingsWindow
  - files

### FILE: SettingsWindow_Events.cs

Types:
  - SettingsWindow

Methods: 8
  - AutoSave()
  - ChkAutoSave_Changed()
  - ChkCheckUpdates_Changed()
  - ChkShowMadeWithSplash_Changed()
  - ChkShowWarnings_Changed()
  - ChkShowWelcome_Changed()
  - CmbLanguage_Changed()
  - SliderUndoHistory_Changed()

### FILE: SettingsWindow_Main.cs

Types:
  - SettingsWindow

Methods: 4
  - CloseButton_Click()
  - OnLoaded()
  - SelectComboByTag()
  - TitleBar_MouseLeftButtonDown()

### FILE: SettingsWindow_UI.cs

Types:
  - SettingsWindow

Methods: 12
  - ApplySettingsToUi()
  - GenerateTargetSections()
  - GetOrCreateEmulatorSettings()
  - NavAppearance_Click()
  - NavEmulators_Click()
  - NavGeneral_Click()
  - NavToolchain_Click()
  - PopulateEmulatorSettings()
  - PopulateLanguageCombo()
  - ShowSection()
  - TabGeneralBehavior_Click()
  - TabGeneralInterface_Click()

### FILE: SplashScreen.xaml.cs

Types:
  - SplashScreen

Methods: 6
  - AddLogEntry()
  - AnimateToComplete()
  - Border_MouseLeftButtonDown()
  - GetAppVersion()
  - RunAsync()
  - UpdateProgress()

### FILE: StateManager.cs

Types:
  - StateManager
  - StateChange
  - ChangeType

Methods: 6
  - ApplyChange()
  - AutoSaveTimer_Tick()
  - Dispose()
  - RegisterChange()
  - SaveNowAsync()
  - SetAutoSaveEnabled()

### FILE: TargetGridControl.xaml.cs

Types:
  - TargetGridControl

Methods: 15
  - BtnGrid_Click()
  - BtnList_Click()
  - BuildGridCard()
  - BuildListCard()
  - Filter_Changed()
  - Initialize()
  - LoadFavorites()
  - OnFavoritesChanged()
  - OnLanguageChanged()
  - RebuildComboBoxes()
  - RenderTargets()
  - SaveFavorites()
  - Sort_Changed()
  - ToggleFavorite()
  - UpdateViewModeButtons()

### FILE: TargetSelectionDialog.xaml.cs

Types:
  - TargetSelectionDialog

Methods: 3
  - Cancel_Click()
  - TargetGrid_TargetSelected()
  - TitleBar_MouseLeftButtonDown()

### FILE: TargetSettingsControl.xaml.cs

Types:
  - TargetSettingsControl

Methods: 4
  - BtnBrowseEmulator_Click()
  - ChkLaunchEmulator_Changed()
  - Initialize()
  - TxtEmulatorArguments_Changed()

### FILE: TilePickerControl.xaml.cs

Types:
  - TilePickerControl

Methods: 13
  - Clear()
  - GetSelectedTiles()
  - LoadFromDefaultFont()
  - LoadFromPng()
  - LoadFromRawBytes()
  - LoadFromSmsVram()
  - LoadFromTileEntries()
  - LoadFromTtf()
  - OnVisualPropertyChanged()
  - RenderGrid()
  - TileGridImage_MouseDown()
  - TileGridImage_MouseMove()
  - TileGridImage_MouseUp()

### FILE: TilesetComposerControl.xaml.cs

Types:
  - TilesetComposerControl

Methods: 7
  - Border_DragOver()
  - Border_Drop()
  - Clear()
  - ComposerImage_MouseDown()
  - LoadComposition()
  - RenderComposer()
  - UpdateStats()

### FILE: TrExtension.cs

Types:
  - TrExtension

Methods: 1
  - ProvideValue()

### FILE: VisualToolInvoker.cs

Types:
  - VisualToolInvoker

Methods: 2
  - Initialize()
  - OpenVisualTool()

### FILE: WelcomeView.xaml.cs

Types:
  - WelcomeView

Methods: 21
  - About_Click()
  - BuildRecentProjectGridCard()
  - BuildRecentProjectListCard()
  - Documentation_Click()
  - HideDropOverlay()
  - LoadRecentProject()
  - MainContent_DragEnter()
  - MainContent_DragLeave()
  - MainContent_Drop()
  - NewProject_Click()
  - OnLoaded()
  - OnTargetSelected()
  - OpenProject_Click()
  - RecentProject_Click()
  - RefreshRecentProjects()
  - RenderRecentProjects()
  - RenderSidebarRecentProjects()
  - ResolveTargetLabel()
  - ShowDropOverlay()
  - UpdateTargetCount()

---

## SUMMARY

Total Projects: 44
Total Files: 484
Total Methods: 1917
