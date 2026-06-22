using Microsoft.Playwright;
using System.Globalization;
using Log = CareAdminTestProject.Common.TestLog;

namespace CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs
{
    /// <summary>
    /// Serves as the abstract base class for all incident form tab pages.
    /// Provides shared locators, input handlers, and utility methods for UI interactions.
    /// </summary>
    /// <para><b>--- METHOD DIRECTORY & QUICK LINKS ---</b></para>
    /// <list type="bullet">
    ///   <item> <description> Form Control Resolvers & Core Utilities </description> </item>
    ///   <list type="bullet">
    ///     <item> <description> Labeled Form Field Locator: <see cref="GetFieldByLabel(string)"/> </description> </item>
    ///     <item> <description> Case-Insensitive Button Locator: <see cref="GetButtonByText(string)"/> </description> </item>
    ///     <item> <description> Raw String Label Value Extractor: <see cref="GetFieldValueByLabelAsync(string)"/> </description> </item>
    ///   </list>
    /// 
    ///   <item> <description> Angular Material & Custom Dropdown Selectors </description> </item>
    ///   <list type="bullet">
    ///     <item> <description> Selected Choice Inner Text Reader: <see cref="GetDropdownValueAsync(string)"/> </description> </item>
    ///     <item> <description> Index-Based Option Core Clicker: <see cref="SelectMatOptionByLabel(string, int)"/> </description> </item>
    ///     <item> <description> Contextual String Dropdown Option Picker: <see cref="SelectDropdownOptionAsync(string, string, int)"/> </description> </item>
    ///     <item> <description> Single-Instance Row Index Dropdown Picker: <see cref="SelectDropdownOptionAsync(string, int)"/> </description> </item>
    ///   </list>
    /// 
    ///   <item> <description> Radio Button, Checkbox & Question Interactions </description> </item>
    ///   <list type="bullet">
    ///     <item> <description> Section-Isolated Panel Radio Choice Toggler: <see cref="SelectRadioOptionAsync(string, string)"/> </description> </item>
    ///     <item> <description> Row-Isolated Questionnaire Field Option Selector: <see cref="SelectQuestionRadioAsync(string, string)"/> </description> </item>
    ///     <item> <description> Multi-Tier Labeled Checkbox Status Interrogator: <see cref="IsCheckboxCheckedAsync(string)"/> </description> </item>
    ///     <item> <description> Group-Scoped Class and Aria Radio Evaluator: <see cref="IsRadioOptionSelectedAsync(string, string)"/> </description> </item>
    ///   </list>
    /// 
    ///   <item> <description> Kendo UI Specialized Picker Components </description> </item>
    ///   <list type="bullet">
    ///     <item> <description> Iframe-Isolated Kendo Rich Text Handler: <see cref="FillRichTextFieldAsync(string, string)"/> </description> </item>
    ///     <item> <description> Labeled Calendar Input Trigger Resolver: <see cref="GetFieldIcon(string)"/> </description> </item>
    ///     <item> <description> Named Datepicker Input Trigger Resolver: <see cref="GetFieldIconByName(string)"/> </description> </item>
    ///     <item> <description> Complex Grid TimePicker Time Dispatcher: <see cref="SelectTimeInPickerAsync(string, TimeOnly)"/> </description> </item>
    ///     <item> <description> Column Regular-Expression Value Matcher: <see cref="SelectKendoColumnValue(ILocator, int, string[])"/> </description> </item>
    ///   </list>
    /// 
    ///   <item> <description> Field & Tab Completeness Indicators </description> </item>
    ///   <list type="bullet">
    ///     <item> <description> Dynamic Completeness Marker ("Red Dot") Resolver: <see cref="GetRedDotLocatorAsync(string)"/> </description> </item>
    ///     <item> <description> Mandatory Verification State Interrogator: <see cref="IsFieldMarkedRequiredAsync(string)"/> </description> </item>
    ///     <item> <description> Kendo TabStrip Header Completeness Badge Verifier: <see cref="IsTabMarkedIncompleteAsync(string)"/> </description> </item>
    ///   </list>
    /// 
    ///   <item> <description> Role Sign-Off & Verification Workflows </description> </item>
    ///   <list type="bullet">
    ///     <item> <description> Administrative Cryptographic Document Signer: <see cref="SignAsRoleAsync(RoleToSign)"/> </description> </item>
    ///     <item> <description> Media-Source Signature Presentation Validator: <see cref="VerifySignatureImageVisible"/> </description> </item>
    ///   </list>
    /// </list>
    public abstract class BaseIncidentTabs
    {
        /// <summary>
        /// The Playwright page instance scoped to the current test context.
        /// </summary>
        public readonly IPage Page;

        /// <summary>
        /// Specifies the authorized medical or administrative roles required to sign off on an incident.
        /// </summary>
        public enum RoleToSign
        {
            /// <summary> Director of Nursing or Designee </summary>
            DNS,
            /// <summary> Medical Director </summary>
            MD,
            /// <summary> Facility Administrator </summary>
            Administrator
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseIncidentTabs"/> class.
        /// </summary>
        /// <param name="page">The Playwright page instance provided by the test suite or parent Page Object.</param>
        public BaseIncidentTabs(IPage page)
        {
            Page = page;
        }

        /// <summary>
        /// Resolves an interactive form element (input, textarea, or mat-select) by filtering within its labeled layout wrapper container.
        /// </summary>
        /// <param name="labelText">The exact user-facing descriptive label string text.</param>
        /// <returns>An <see cref="ILocator"/> targeting the input control inside the labeled component.</returns>
        public ILocator GetFieldByLabel(string labelText)
        {
            // Берём твой исходный базовый локатор
            var locator = Page.Locator("cad-label-value-field")
                              .Filter(new() { HasText = labelText });

            // Если мы ищем НЕ поле "Name", то принудительно исключаем из результатов 
            // блок выбора резидента, чтобы фамилии вроде "Bednar" не ломали strict mode
            if (labelText != "Name")
            {
                locator = locator.Filter(new()
                {
                    // Исключаем контейнер, если внутри него находится кастомный селект резидента
                    HasNot = Page.Locator("cad-lookup-select, rnt-resident-lookup-select")
                });
            }

            // Возвращаем инпут/селект как в твоем исходном коде
            return locator.Locator("input, textarea, mat-select");
        }

        /// <summary>
        /// Resolves an actionable button element matched by its precise text label, ignoring string casing boundaries.
        /// </summary>
        /// <param name="buttonText">The text displayed on the target button.</param>
        /// <returns>An <see cref="ILocator"/> pointing to the button component.</returns>
        public ILocator GetButtonByText(string buttonText)
        {
            // Searches specifically for a button with the specified text (case-insensitive)
            return Page.GetByRole(AriaRole.Button, new() { Name = buttonText });
        }

        /// <summary>
        /// Interacts with an embedded iframe inside a Kendo UI Rich Text editor component to sequentially input plaintext values.
        /// </summary>
        /// <param name="fieldName">The unique html name attribute identifier assigned to the kendo-editor element.</param>
        /// <param name="text">The string data content payload to type into the editor canvas.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Obsolete("Use newer non-iframe ProseMirror interaction handlers if the application framework updates.")]
        public async Task FillRichTextFieldAsync(string fieldName, string text)
        {
            // 1. Locate the custom kendo-editor component element itself. 
            // The name="summary" or name="summaryPlan" attributes are typically present in the DOM.
            var editor = Page.Locator($"kendo-editor[name='{fieldName}']");

            // 2. Initialize the FrameLocator instance. 
            // Note: FrameLocator is lazy by design; it defers resolution until internal nested elements are queried.
            var frame = editor.FrameLocator("iframe");

            // 3. Resolve the target body layout node inside the iframe context. 
            // Within Kendo Editor setups, the body element possesses the contenteditable="true" attribute.
            var editableArea = frame.Locator("body");

            // 4. Synchronize thread until the frame area registers as stable and fully accessible to browser events.
            // Using State.Visible or explicitly awaiting specific layout attributes is recommended for iframe elements.
            await editableArea.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

            // 5. Shift focus to the editable area canvas. Triggering a ClickAsync is often more reliable than FocusAsync.
            await editableArea.ClickAsync();

            // Inject the string character sequence sequentially
            await editableArea.PressSequentiallyAsync(text, new() { Delay = 50 });

            // NOTE: Review debug log
            Log.Debug($"Field '{fieldName}' filled.");
        }

        /// <summary>
        /// Reads and returns the active string text content residing inside a standard text input field resolved by its label.
        /// </summary>
        /// <param name="label">The descriptive label text linked to fields like Room, Bed, or SBARSummary.</param>
        /// <returns>The string value currently residing inside the input field.</returns>
        public async Task<string> GetFieldValueByLabelAsync(string label)
        {
            // Reads the value from a regular text input (Room, Bed, SBARSummary)
            return await GetFieldByLabel(label).InputValueAsync();
        }

        /// <summary>
        /// Retrieves the currently selected string description value from an active custom Angular dropdown select container component.
        /// </summary>
        /// <param name="label">The descriptive header label identifying the target dropdown select control (e.g., Unit, Location, Type).</param>
        /// <returns>The selected choice inner text value string.</returns>
        public async Task<string> GetDropdownValueAsync(string label)
        {
            // Reads the selected text or value from an Angular dropdown component.
            // The exact strategy depends on your select component architecture, for example:
            return await Page.Locator($"[data-label='{label}'] .selected-value").InnerTextAsync();
        }

        /// <summary>
        /// Locates the clickable toggle control button used to trigger Kendo UI calendar dropdown overlays.
        /// </summary>
        /// <param name="labelText">The unique descriptive label text associated with the form field.</param>
        /// <returns>An <see cref="ILocator"/> pointing directly to the input button control wrapper element.</returns>
        protected ILocator GetFieldIcon(string labelText)
        {
            // Isolate the parent container wrapper by its label text, then locate the core internal Kendo input trigger
            return Page.Locator("kendo-formfield")
                        .Filter(new() { HasText = labelText })
                        .Locator("button.k-input-button");
        }

        /// <summary>
        /// Locates the clickable Kendo UI date picker icon trigger element based on its technical html name attribute.
        /// </summary>
        /// <param name="nameAttribute">The specific string value assigned to the name attribute of the kendo-datepicker element.</param>
        /// <returns>An <see cref="ILocator"/> pointing to the input button control wrapper element.</returns>
        public ILocator GetFieldIconByName(string nameAttribute)
        {
            // Search for kendo-datepicker with the matching name and find the calendar button inside it
            return Page.Locator($"kendo-datepicker[name='{nameAttribute}'] button.k-input-button");
        }

        /// <summary>
        /// Opens a material select dropdown or Kendo list filtered by its label text and selects an option based on its zero-based index position.
        /// </summary>
        /// <param name="labelText">The exact user-facing descriptive label string text.</param>
        /// <param name="index">The zero-based row index position of the item option within the expanded list overlay.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task SelectMatOptionByLabel(string labelText, int index)
        {
            var field = GetFieldByLabel(labelText);
            await field.ClickAsync();

            // Wait for the option collection overlay layer list to render inside the viewport
            var options = Page.Locator("mat-option, .k-item");
            await options.First.WaitForAsync();

            // Select item row matching targeted index configuration rules
            await options.Nth(index).ClickAsync();
        }

        /// <summary>
        /// Performs the specific role authorization workflow sequence by expanding signature slots and committing confirmation events.
        /// </summary>
        /// <param name="role">The operational administrative or nursing authorization role enum reference property.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided role assignment evaluation fails to map to known signature slot configurations.</exception>
        public async Task SignAsRoleAsync(RoleToSign role)
        {
            // Mapping enum configurations to the exact textual strings displayed inside the signature layout forms
            string roleText = role switch
            {
                RoleToSign.DNS => "Director of Nursing",
                RoleToSign.MD => "Medical Director",
                RoleToSign.Administrator => "Administrator",
                _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unknown signature role configuration value: {role}")
            };

            // NOTE: Review debug log
            Log.Debug($"[SIGNATURE] Starting sign-off sequence workflow for role assignment type: {roleText}...");

            // Resolve the dedicated cad-incident-sign container element block filtered by its role title inner text
            var signatureContainer = Page.Locator("cad-incident-sign")
                .Filter(new() { HasText = roleText })
                .First;

            var signButton = signatureContainer.Locator("button:has-text('Sign Here')");

            // Perform scrolling alignment checks and trigger click actions on the primary sign trigger control node
            await signButton.ScrollIntoViewIfNeededAsync();
            await signButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await signButton.ClickAsync();
            // NOTE: Review debug log
            Log.Debug($"The 'Sign Here' action click button for role {roleText} was triggered successfully.");

            // NOTE: Review debug log
            Log.Debug("Awaiting the rendering cycles of the active Confirm Signature overlay modal dialog wrapper template...");
            var confirmDialog = Page.Locator("cad-incident-confirm-sign-dialog");
            await confirmDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // Locate and execute click events on the explicit confirm action button control embedded inside the modal window container layers
            var confirmButton = confirmDialog.Locator("button:has-text('Confirm')");
            await confirmButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
            await confirmButton.ClickAsync();
            // NOTE: Review debug log
            Log.Debug($"The confirmation action 'Confirm' button for role {roleText} was triggered successfully.");

            // Wait until the modal dialog layout completes its dismissal transition sequence and leaves the viewport entirely
            await confirmDialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
            // NOTE: Review debug log
            Log.Debug($"The signature authorization process workflow for role {roleText} has completed successfully.");
        }

        /// <summary>
        /// Validates that the capture signature graphic asset box has rendered properly and contains valid media reference sources.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown if the target image locator registers as visible but lacks an operational graphic source URL path attribute.</exception>
        public async Task VerifySignatureImageVisible()
        {
            // 1. Isolate the target selector context containing the signature rendering graphic asset (.signature-box class wrapper)
            var signatureImage = Page.Locator(".signature-box img");

            // 2. Validate that the target graphic asset not only resides in the DOM hierarchy tree, but maps as active and visible inside the browser layers
            await Assertions.Expect(signatureImage).ToBeVisibleAsync(new() { Timeout = 10000 });

            // 3. Supplementary verification evaluation step: inspect the object parameters to verify a valid source reference location is populated
            var src = await signatureImage.GetAttributeAsync("src");
            if (string.IsNullOrEmpty(src))
            {
                throw new Exception("The signature layout verification expects a graphic node, but the source asset URL string (src) attribute evaluates as empty.");
            }

            // NOTE: Review debug log
            Log.Information("The validation signature object successfully persists and renders correctly as an image element asset.");
        }

        /// <summary>
        /// Selects an option text from an Angular material dropdown field component isolated by its label text and an index position context.
        /// </summary>
        /// <param name="labelText">The exact user-facing descriptive label string text.</param>
        /// <param name="optionText">The target option choice text to look for and select inside the expanded overlay.</param>
        /// <param name="indexInList">The zero-based index of the dropdown component instance matching this label description.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SelectDropdownOptionAsync(string labelText, string optionText, int indexInList = 0)
        {
            // Find the i-th dropdown element matching this label description syntax rules
            var dropdown = GetFieldByLabel(labelText).Nth(indexInList);

            await dropdown.ClickAsync();

            // The options overlay layer is usually unique and generated at the body root level at a single instance in time
            var option = Page.Locator("mat-option", new() { HasText = optionText }).First;

            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await option.ClickAsync();
        }

        /// <summary>
        /// Opens an Angular material dropdown select component and selects an item option matching the given row sequence index.
        /// </summary>
        /// <param name="labelText">The exact user-facing descriptive label string text.</param>
        /// <param name="index">The one-based row index position pointing to the target option element choice within the expanded container wrapper view.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SelectDropdownOptionAsync(string labelText, int index)
        {
            var dropdown = GetFieldByLabel(labelText).First;
            await dropdown.ClickAsync();

            // Search for target elements inside the currently active expanded list popup container layer
            var option = Page.Locator(".cdk-overlay-container mat-option:visible").Nth(index - 1);

            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await option.ClickAsync();

            // Wait until the container modal overlay closes completely to prevent rendering conflicts with consecutive test executions
            await Page.Locator(".cdk-overlay-container").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        /// <summary>
        /// Opens a Kendo UI time picker popup window grid and configures specific hours, minutes, and AM/PM time properties.
        /// </summary>
        /// <param name="nameAttribute">The specific string value assigned to the name attribute of the kendo-timepicker element.</param>
        /// <param name="time">The structural time value components to assign sequentially.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SelectTimeInPickerAsync(string nameAttribute, TimeOnly time)
        {
            var pickerContainer = Page.Locator($"kendo-timepicker[name='{nameAttribute}']");
            await pickerContainer.Locator("button.k-input-button").ClickAsync();

            var popup = Page.Locator("kendo-popup:visible, .k-animation-container:visible").First;
            await popup.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            await Task.Delay(200);

            // Prepare both padding string layouts for hours formatting evaluations: "1" and "01" options
            var hourSingle = time.ToString("%h", CultureInfo.InvariantCulture);
            var hourDouble = time.ToString("hh", CultureInfo.InvariantCulture);

            var minute = time.ToString("mm", CultureInfo.InvariantCulture);
            var amPm = time.ToString("tt", CultureInfo.InvariantCulture).ToUpper();

            // Pass the parameter arrays forward into the column picker configuration value selectors
            await SelectKendoColumnValue(popup, 0, hourSingle, hourDouble);
            await SelectKendoColumnValue(popup, 1, minute);
            await SelectKendoColumnValue(popup, 2, amPm);

            await popup.GetByRole(AriaRole.Button, new() { Name = "Set" }).DispatchEventAsync("click");
            await popup.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        /// <summary>
        /// Вводит время вручную с клавиатуры в инпут времени.
        /// </summary>
        public async Task TypeTimeManuallyAsync(TimeOnly time)
        {
            // Находим сам kendo-timepicker по его имени и извлекаем внутренний инпут
            var pickerContainer = Page.Locator($"kendo-timepicker[name='answerTime']");
            var inputLocator = pickerContainer.Locator("input.k-input-inner").First;

            await inputLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            // Кликаем по инпуту и выделяем весь текст (Ctrl+A), чтобы стереть старое значение
            await inputLocator.ClickAsync();
            await Page.Keyboard.PressAsync("Control+A");
            await Page.Keyboard.PressAsync("Delete");

            // Форматируем время в строку без двоеточий, так как Kendo-маска сама расставит разделители
            // Например, для 15:15 -> "0315PM", для 09:00 -> "0900AM"
            string timeStringToType = time.ToString("hhmmtt", System.Globalization.CultureInfo.InvariantCulture);

            Console.WriteLine($"[FILL] Вводим текст '{timeStringToType}' в Kendo TimePicker.");

            // Эмулируем посимвольный ввод с клавиатуры
            await Page.Keyboard.TypeAsync(timeStringToType);

            // Нажимаем Tab, чтобы зафиксировать значение и убрать фокус
            await Page.Keyboard.PressAsync("Tab");
        }

        /// <summary>
        /// Проверяет текущее текстовое значение внутри инпута Kendo UI TimePicker.
        /// </summary>
        public async Task VerifyTimeFieldValueAsync(TimeOnly expectedTime)
        {
            var pickerContainer = Page.Locator($"kendo-timepicker[name='answerTime']");
            var inputLocator = pickerContainer.Locator("input.k-input-inner").First;

            // Kendo ожидает строку с разделителями, например "03:15 PM"
            string expectedTimeString = expectedTime.ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

            // Проверяем реальное value инпута
            await Assertions.Expect(inputLocator).ToHaveValueAsync(expectedTimeString);
            Console.WriteLine($"[FIELD-CHECK] Время в Kendo инпуте совпадает с ожидаемым: {expectedTimeString}");
        }

        /// <summary>
        /// Locates a specific time list column inside an active Kendo popup window picker and dispatches click event handlers onto target row string options using exact match patterns.
        /// </summary>
        /// <param name="popup">The locator context pointing to the visible active Kendo popup window component.</param>
        /// <param name="columnIndex">The zero-based column position identifier within the time picker grid layout.</param>
        /// <param name="values">The variable length array of alternative string selection parameters matching target options.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectKendoColumnValue(ILocator popup, int columnIndex, params string[] values)
        {
            var column = popup.Locator(".k-time-list").Nth(columnIndex);

            // Build an explicit exact match pattern structure evaluating any of the provided tracking options
            var pattern = $"^({string.Join("|", values)})$";

            // Pass the generated regular expression criteria instance directly to the GetByText finder component
            var item = column.Locator(".k-item")
                .GetByText(new Regex(pattern))
                .First;

            await item.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await item.ScrollIntoViewIfNeededAsync();
            await item.DispatchEventAsync("click");

            // NOTE: Review debug log
            Log.Debug($"[SUCCESS] Value from choice list collection [{string.Join(", ", values)}] successfully selected.");

            await Task.Delay(100);
        }

        /// <summary>
        /// Identifies a specific parent state configuration panel section layout by its header text and checks a target radio button value choice option inside it.
        /// </summary>
        /// <param name="sectionLabel">The exact string header label defining the targeted physical form block.</param>
        /// <param name="optionValue">The text label value string associated with the specific target option choice button component (e.g., "Yes" or "No").</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SelectRadioOptionAsync(string sectionLabel, string optionValue)
        {
            if (string.IsNullOrEmpty(optionValue)) return;

            // 1. Locate the panel text header element node
            var header = Page.Locator(".state-panel__header")
                .GetByText(sectionLabel, new() { Exact = true });

            // 2. Isolate the explicit organizational panel wrapper container (parent framework structure) to constrain consecutive selector queries within it
            var panel = Page.Locator(".state-panel")
                .Filter(new() { Has = Page.Locator(".state-panel__header").GetByText(sectionLabel, new() { Exact = true }) });

            // 3. Search inside this isolated panel segment context for a radio button element containing the matching choice label text
            var radioButton = panel.Locator("mat-radio-button")
                .GetByText(optionValue, new());

            // NOTE: Review debug log
            Log.Debug($"RadioButton {sectionLabel} is set as {optionValue}");

            // Execute click action hooks directly on the resolved mat-radio-button component instance
            await radioButton.ClickAsync();
        }

        // Reset radio button configurations within a specific localized section container filtered by its header section title

        /// <summary>
        /// Filters form layouts to find a questionnaire field container matching the target text string, then toggles its associated exact choice value radio option button element.
        /// </summary>
        /// <param name="questionText">The exact descriptive question prompt string text residing inside the form wrapper container.</param>
        /// <param name="optionValue">The exact textual selection choice name title string (e.g., "Unavoidable").</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SelectQuestionRadioAsync(string questionText, string optionValue)
        {
            if (string.IsNullOrEmpty(optionValue)) return;

            // 1. Isolate the specific row container layout matching the target question text description (.question-field class wrapper block)
            var questionContainer = Page.Locator(".question-field")
                .Filter(new() { HasText = questionText });

            // 2. Search inside this isolated field wrapper context for a material radio button component matching choice parameters
            // Querying via mat-radio-button is highly resilient since standard GetByText structures resolve nested text nodes or label properties accurately
            var radioButton = questionContainer.Locator("mat-radio-button")
                .GetByText(optionValue, new() { Exact = true });

            // 3. Ensure target element alignment is forced into view prior to dispatching click triggers
            await radioButton.ScrollIntoViewIfNeededAsync();
            await radioButton.ClickAsync();

            // NOTE: Review debug log
            Log.Debug($"Radio '{optionValue}' selected for question: {questionText.Substring(0, 20)}...");
        }

        /// <summary>
        /// Resolves the completeness indicator element locator ("Red Dot") matching a field descriptor or dynamic question text.
        /// </summary>
        /// <param name="fieldName">The target field description or question text string used to locate the layout wrapper.</param>
        /// <returns>An <see cref="ILocator"/> pointing to the target field's completeness indicator span component.</returns>
        public async Task<ILocator> GetRedDotLocatorAsync(string fieldName)
        {
            // === НАЧАЛО ЖЕЛЕЗОБЕТОННОЙ ВСТАВКИ ПО ВЕРСТКЕ ANGULAR ===
            if (fieldName.StartsWith("Injuries Sustained") && fieldName != "Injuries Sustained")
            {
                Log.Debug($"[InjuryRedDot Diagnostic] Entered dynamic branch for field: '{fieldName}'");

                var numberString = fieldName.Replace("Injuries Sustained", "").Trim();
                if (int.TryParse(numberString, out int rowNumber))
                {
                    int index = rowNumber - 1; // 2 превращаем в индекс 1 для Playwright
                    Log.Debug($"[InjuryRedDot Diagnostic] Parsed row number: {rowNumber}, target index: {index}");

                    // 1. Находим все строки травм по стабильному классу из верстки
                    var allRows = Page.Locator("div.injury-line");

                    int rowsCount = await allRows.CountAsync();
                    Log.Debug($"[InjuryRedDot Diagnostic] Total physical injury lines found: {rowsCount}");

                    // 2. Выбираем нужную по индексу строку
                    var targetRow = allRows.Nth(index);

                    // 3. Внутри этой строки берем САМОЕ ПЕРВОЕ поле cad-label-value-field (это всегда Injuries Sustained)
                    var firstFieldInRow = targetRow.Locator("cad-label-value-field").Nth(0);

                    // 4. Забираем его красную точку
                    var indicator = firstFieldInRow.Locator("span.completeness-indicator, span.red-dot, .k-required");

                    int indicatorsCount = await indicator.CountAsync();
                    Log.Debug($"[InjuryRedDot Diagnostic] Indicators found inside targeted row field: {indicatorsCount}");

                    if (indicatorsCount > 0)
                    {
                        bool isVisible = await indicator.First.IsVisibleAsync();
                        Log.Debug($"[InjuryRedDot Diagnostic] Target indicator visibility status: {isVisible}");
                        return indicator.First;
                    }

                    Log.Debug($"[InjuryRedDot Diagnostic] WARNING: Indicator not found in row. Returning empty locator.");
                    return indicator;
                }
            }
            // === КОНЕЦ ВСТАВКИ ПО ВЕРСТКЕ ===


            // 1. Check exact string matching configurations for Summary or Plan fields
            bool isSummaryField = fieldName.Equals("Summary", StringComparison.OrdinalIgnoreCase) ||
                                  fieldName.Equals("Enter summary", StringComparison.OrdinalIgnoreCase);

            bool isPlanField = fieldName.Equals("Plan", StringComparison.OrdinalIgnoreCase) ||
                               fieldName.Equals("Enter plan", StringComparison.OrdinalIgnoreCase);

            if (isSummaryField || isPlanField)
            {
                // NOTE: Review debug log
                Log.Debug($"[RedDot Diagnostic] Entered RichText branch evaluation for field: '{fieldName}'");

                var container = Page.Locator("cad-incident-edit-summary");
                var allWrappers = container.Locator("div.editor-wrapper");
                int wrappersCount = await allWrappers.CountAsync();
                // NOTE: Review debug log
                Log.Debug($"[RedDot Diagnostic] Total 'div.editor-wrapper' elements found: {wrappersCount}");

                // Output real textual metrics of all resolved workspace wrappers for layout tracking
                for (int i = 0; i < wrappersCount; i++)
                {
                    var currentText = await allWrappers.Nth(i).InnerTextAsync();
                    // NOTE: Review debug log
                    Log.Debug($"[RedDot Diagnostic] Container #{i} InnerText: '{currentText?.Replace("\n", " ")}'");
                }

                string searchKeyword = isSummaryField ? "Summary" : "Plan";

                // Utilize a flexible regular expression rule to look up target keywords inside parent container text boundaries
                var richTextPattern = new Regex($@"\b{searchKeyword}\b", RegexOptions.IgnoreCase);
                var targetWrapper = allWrappers.Filter(new() { HasTextRegex = richTextPattern });

                int matchedCount = await targetWrapper.CountAsync();
                // NOTE: Review debug log
                Log.Debug($"[RedDot Diagnostic] Match count remaining after regex filtering for '{searchKeyword}': {matchedCount}");

                var indicator = targetWrapper.First.Locator("span.completeness-indicator");

                bool exists = await indicator.CountAsync() > 0;
                bool isVisible = exists && await indicator.IsVisibleAsync();
                // NOTE: Review debug log
                Log.Debug($"[RedDot Diagnostic] Result evaluation metrics for '{fieldName}': present in DOM = {exists}, visible in view = {isVisible}");

                return indicator;
            }

            // 2. Custom isolated branch workflows for specific question fields and high-complexity form sections (Conclusion, Evidence)
            if (fieldName.Equals("Conclusion Reached", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Equals("Evidence", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Equals("Evidence Reason", StringComparison.OrdinalIgnoreCase) ||
                fieldName.StartsWith("This will be reported", StringComparison.OrdinalIgnoreCase)) // Intercept the long multi-line text question block for DOH reporting
            {
                // Process execution if target items resolve to standard radio input selection blocks (Conclusion, Evidence, or DOH questions)
                if (!fieldName.Equals("Evidence Reason", StringComparison.OrdinalIgnoreCase))
                {
                    string keyword;

                    if (fieldName.Equals("Conclusion Reached", StringComparison.OrdinalIgnoreCase))
                        keyword = "conclusion";
                    else if (fieldName.Equals("Evidence", StringComparison.OrdinalIgnoreCase))
                        keyword = "evidence of abuse";
                    else
                        keyword = "reported to the DOH"; // Identification keyword string to capture and isolate the DOH/OHMS form block

                    var questionPattern = new Regex(keyword, RegexOptions.IgnoreCase);

                    return Page.Locator("cad-incident-edit-summary")
                               .Locator("div.question-field")
                               .Filter(new() { HasTextRegex = questionPattern })
                               .Locator("span.completeness-indicator");
                }

                // Process execution if target items match internal nested RichText editor frameworks for Evidence Reason configurations ("Explain reasoning...")
                if (fieldName.Equals("Evidence Reason", StringComparison.OrdinalIgnoreCase))
                {
                    return Page.Locator("cad-incident-edit-summary")
                               .Locator("div.editor-wrapper")
                               // Используем LocatorOptions для фильтрации по тексту внутри span
                               .Filter(new() { Has = Page.Locator("span", new() { HasText = "Evidence Reason" }) })
                               .Locator("span.completeness-indicator.reason");
                }
            }


            // 3. STANDARD FALLBACK EVALUATION WORKFLOWS FOR ALL REMAINING FORM FIELDS (UNMODIFIED LOGIC)
            int requestedIndex = 0;
            string realLabel = fieldName;

            if (fieldName.Contains("(Relative)"))
            {
                requestedIndex = 0;
                realLabel = fieldName.Replace(" (Relative)", "");
            }
            else if (fieldName.Contains("(MD)"))
            {
                requestedIndex = 1;
                realLabel = fieldName.Replace(" (MD)", "");
            }

            var escapedLabel = Regex.Escape(realLabel).Replace("'", ".*");
            var patternStandard = new Regex(escapedLabel);

            var allFields = Page.Locator("cad-label-value-field")
                                .Filter(new() { HasTextRegex = patternStandard });

            int count = await allFields.CountAsync();

            return requestedIndex == 0
                ? allFields.First.Locator("span.completeness-indicator")
                : count > 1
                    ? allFields.Nth(1).Locator("span.completeness-indicator")
                    : allFields.First.Locator("span.completeness-indicator");
        }

        /// <summary>
        /// Evaluates whether a specific form field registers as required by checking the visibility of its completeness indicator.
        /// </summary>
        /// <param name="fieldName">The target field description or question text string.</param>
        /// <returns>True if the field completeness indicator is visible; otherwise, false.</returns>
        public async Task<bool> IsFieldMarkedRequiredAsync(string fieldName)
        {
            var locator = await GetRedDotLocatorAsync(fieldName);
            return await locator.IsVisibleAsync();
        }

        /// <summary>
        /// Checks the visibility of the completeness indicator badge residing directly on a specific tab name label inside the Kendo TabStrip layout bar.
        /// </summary>
        /// <param name="tabName">The exact visible string text name of the target form tab item.</param>
        /// <returns>True if the completeness indicator on the tab header element is visible; otherwise, false.</returns>
        public async Task<bool> IsTabMarkedIncompleteAsync(string tabName)
        {

            // Within Kendo UI frameworks, tab headers render as li items mapped with a tab role property
            return await Page.Locator("li[role='tab']")
                .Filter(new() { HasText = tabName })
                .Locator("span.completeness-indicator")
                .IsVisibleAsync();
        }

        /// <summary>
        /// Reads the runtime check status of a standard mat-checkbox component wrapper with multi-tier layout fallbacks based on container labels.
        /// </summary>
        /// <param name="label">The exact inner text identifier string of the target checkbox component group.</param>
        /// <returns>True if the underlying native checkbox input registers as checked; otherwise, false.</returns>
        public async Task<bool> IsCheckboxCheckedAsync(string label)
        {
            // 1. Isolate the field container bounding box using a precise text regex match against the layout span labels
            var checkboxFieldContainer = Page.Locator("div.checkbox-field")
                .Filter(new() { HasTextRegex = new Regex($"^{label}$", RegexOptions.IgnoreCase) })
                .First;

            // 2. Invoke a structural recovery fallback check if layout design rules vary across specific forms or sheets
            if (await checkboxFieldContainer.CountAsync() == 0)
            {
                checkboxFieldContainer = Page.Locator("mat-checkbox, cad-label-value-field")
                    .Filter(new() { HasText = label })
                    .First;
            }

            // 3. Resolve the material checkbox structure and extract metrics directly out of the hidden native input node
            var checkboxInput = checkboxFieldContainer.Locator("mat-checkbox input");

            return await checkboxInput.IsCheckedAsync();
        }

        /// <summary>
        /// Evaluates whether a specific radio option button choice within a designated group element is currently enabled and chosen.
        /// </summary>
        /// <param name="groupName">The target HTML name attribute string of the radio group (e.g., 'ambulatoryStatus').</param>
        /// <param name="optionValue">The exact textual selection choice name title string configuration to evaluate.</param>
        /// <returns>True if the designated choice element class or aria attributes register as active; otherwise, false.</returns>
        public async Task<bool> IsRadioOptionSelectedAsync(string groupName, string optionValue)
        {
            // 1. Locate the parent radio selection group locator container
            var radioGroup = Page.Locator("mat-radio-group[name='ambulatoryStatus'], mat-radio-group");

            if (await radioGroup.CountAsync() > 1)
            {
                radioGroup = radioGroup.Filter(new() { HasTextRegex = new Regex("Ambulatory", RegexOptions.IgnoreCase) });
            }

            string cleanOptionValue = optionValue.Trim();

            var radioButton = radioGroup.Locator("mat-radio-button, mat-mdc-radio-button")
                .Filter(new() { HasTextRegex = new Regex(cleanOptionValue, RegexOptions.IgnoreCase) })
                .First;

            // Ensure layout elements finish rendering inside the viewport boundaries
            await radioButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // 3. Read active state metrics directly from layout style classes and accessibility properties
            string classAttribute = await radioButton.GetAttributeAsync("class") ?? "";
            string ariaChecked = await radioButton.GetAttributeAsync("aria-checked") ?? "false";

            return classAttribute.Contains("mat-mdc-radio-checked")
                   || classAttribute.Contains("mat-radio-checked")
                   || ariaChecked.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
