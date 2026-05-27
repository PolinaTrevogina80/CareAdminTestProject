using CareAdminTestProject.Common;
using Microsoft.Playwright;
using static GeneralTab;
using Log = CareAdminTestProject.Common.TestLog;


namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    [TestFixture]
    public class IncidentGeneralTests : BaseIncidentTests
    {
        #region 1. Asterisk Validation (Minimum for creation)

        /// <summary>
        /// Verifies that the 'Create' button remains disabled when the form is opened and mandatory fields are empty.
        /// </summary>
        [Test]
        [Description("Verify that the 'Create' button is disabled until all asterisk-marked fields are filled.")]
        public async Task Test_CreateButtonLock_UntilFieldsFilled()
        {
            await steps.VerifyCreateButtonStateAsync(shouldBeEnabled: false);
        }

        /// <summary>
        /// Validates that filling out the minimum set of required (asterisk-marked) fields successfully activates the 'Create' button.
        /// </summary>
        [Test]
        [Description("Sequentially fill the asterisk-marked fields -> verify that the 'Create' button becomes active.")]
        public async Task Test_MinimumSetCompletion_ActivatesCreateButton()
        {
            var minimalData = data with { General = data.General.GetOnlyRequiredFields() };
            await steps.FillGeneralTabAsync(minimalData);
            await steps.VerifyCreateButtonStateAsync(shouldBeEnabled: true);
        }

        /// <summary>
        /// Ensures that the Kendo UI date and time pickers block user selection of any future dates or times based on system constraints.
        /// </summary>
        [Test]
        [Description("Verify that the Kendo UI calendar and time picker strictly prevent selecting future dates and times via UI constraints.")]
        public async Task Test_DateValidation_PreventsFutureDateTime()
        {
            Log.Information("Starting test: UI-Driven Future Date and Time Validation");
            await steps.VerifyTomorrowIsDisabledInCalendarAsync();
            await steps.VerifyFutureTimeIsDisabledInPickerAsync();
            Log.Information("All future-time UI constraints successfully verified.");
        }

        #endregion

        #region 3. Auto-population and Dependencies

        /// <summary>
        /// Verifies that resident profile details (Room, Bed, Unit) are correctly auto-populated upon resident selection while other fields remain unaffected.
        /// </summary>
        [Test]
        [Description("Verify that Room, Bed, and Unit fields are automatically pulled from the selected resident's profile.")]
        public async Task Test_ResidentData_AutoPopulation()
        {
            Log.Debug("Starting test: Resident Data Auto-population");
            var expectedAutopopulated = data.General with
            {
                date = null,
                time = null,
                location = string.Empty,
                type = string.Empty,
                activity = string.Empty,
                summary = string.Empty,
                supervisor = null,
                chargeNurse = null,
                cna = null
            };
            await steps.VerifyDataRetainedAsync(expectedAutopopulated);
            Log.Information("Resident profile auto-population fields constraints successfully verified.");
        }

        /// <summary>
        /// Compares the count of active facility staff members in UI dropdowns against the API backend data response to ensure proper filtering.
        /// </summary>
        [Test]
        [Description("Verify that staff dropdowns correctly filter current employees based on their employment status (termDate).")]
        public async Task Test_StaffLists_DisplayCorrectFacilityEmployeesCount()
        {
            Log.Information("Starting test: Staff Lists Validation via API Counters");

            await steps.VerifyStaffDropdownsCountWithApiAsync();
        }

        #endregion

        #region 4. Injuries checks

        /// <summary>
        /// Validates inline validation behavior for the first injury row, checking the focus-blur required error message state and the visual red dot indicator.
        /// </summary>
        [Test]
        public async Task InjurySection_FirstRowValidation_AndBlurEffect()
        {
            var fieldSustained = steps.CreatePage.GetFieldByLabel("Injuries Sustained").Nth(0);

            await steps.VerifyCreateButtonStateAsync(false);
            await steps.VerifyRedDotField(steps.CreatePage.General, "Injuries Sustained", true);

            await fieldSustained.FocusAsync();
            await Page.ClickAsync("body");

            await Assertions.Expect(Page.Locator("text=Required field")).ToBeVisibleAsync();
            var testInjury = new InjuryInfo("Laceration", null, null, null, null);
            await steps.CreatePage.General.AddInjuryAsync(0, testInjury);
            await steps.VerifyRedDotField(steps.CreatePage.General, "Injuries Sustained", false);
        }

        /// <summary>
        /// Verifies that deleting all injury rows locks form submission and displays a global section validation error message.
        /// </summary>
        [Test]
        public async Task InjurySection_DeleteAllInjuries_ShouldShowGlobalError()
        {
            await steps.CreatePage.GetButtonByText("Delete").Nth(0).ClickAsync();
            await steps.VerifyCreateButtonStateAsync(false);
            var globalError = Page.Locator("text=Please add at least one injury");
            await Assertions.Expect(globalError).ToBeVisibleAsync();
        }

        /// <summary>
        /// Confirms that multiple dynamic injury fields can be appended, populated sequentially, and mapped correctly via the dynamic fields handler.
        /// </summary>
        [Test]
        public async Task CompleteIncidentForm_WithMultipleInjuries_Verification()
        {
            var tab = "General";
            var testData = data with
            {
                General = data.General with
                {
                    injury = new List<InjuryInfo>
                    {
                        new InjuryInfo("Laceration", "Hand", "3", null, null),
                        new InjuryInfo("Hematoma", "Head", "5", null, null)
                    }
                }
            };

            await steps.ClearGeneralForm();

            for (int i = 1; i < testData.General.injury.Count; i++)
            {
                await steps.CreatePage.GetButtonByText("Add Injury").ClickAsync();
                await Task.Delay(300);
            }

            await steps.VerifyFieldsOneByOneWithFilling(steps.CreatePage.General, testData.General);
        }

        /// <summary>
        /// Checks the numerical input field (Length) for invalid keyboard stroke rejections (non-numeric characters) and successful floating/negative value bindings.
        /// </summary>
        [Test]
        public async Task InjurySection_NumericFieldsInputValidation_Verification()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };

            await steps.FillGeneralTabAsync(minimalData);

            var lengthField = steps.CreatePage.GetFieldByLabel("Length (Centimeters)").Nth(0);

            var initialValue = await lengthField.InputValueAsync();

            await lengthField.FocusAsync();
            await lengthField.PressSequentiallyAsync("abc");

            await Assertions.Expect(lengthField).ToHaveValueAsync(initialValue);

            await lengthField.ClearAsync();
            await lengthField.FillAsync("10.5");
            await Assertions.Expect(lengthField).ToHaveValueAsync("10.5");

            await lengthField.ClearAsync();
            await lengthField.FillAsync("-5");
            await Assertions.Expect(lengthField).ToHaveValueAsync("-5");
        }

        /// <summary>
        /// Verifies that deleting an intermediate row correctly forces a UI index shift recalculation, shifting subsequent rows up to fill the sequence without losing form validity.
        /// </summary>
        [Test]
        public async Task InjurySection_RowDeletionAndIndexRecalculation_Verification()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);

            var firstInjury = new InjuryInfo("Laceration", "Arm", "5.5", null, null);
            await steps.CreatePage.General.AddInjuryAsync(0, firstInjury);
            await steps.VerifyCreateButtonStateAsync(true);

            await steps.CreatePage.GetButtonByText("Add Injury").ClickAsync();
            await steps.VerifyCreateButtonStateAsync(false);

            var secondInjury = new InjuryInfo("Contusion", "Leg", "12", null, null);
            await steps.CreatePage.General.AddInjuryAsync(1, secondInjury);

            await steps.VerifyCreateButtonStateAsync(true);
            await steps.CreatePage.GetButtonByText("Delete").Nth(0).ClickAsync();

            var sustainedField = steps.CreatePage.GetFieldByLabel("Injuries Sustained").Nth(0);
            var lengthField = steps.CreatePage.GetFieldByLabel("Length (Centimeters)").Nth(0);
            var remainingSustainedText = (await sustainedField.TextContentAsync())?.Trim();
            var remainingLengthValue = await lengthField.InputValueAsync();

            Assert.That(remainingSustainedText, Is.EqualTo("Contusion"));
            Assert.That(remainingLengthValue, Is.EqualTo("12"));
            await steps.VerifyCreateButtonStateAsync(true);
        }
        /// <summary>
        /// Verifies that removing an injury row before saving retains and correctly persists only the remaining injury data in the database draft.
        /// </summary>
        [Test]
        public async Task InjurySection_PartialDeletion_ShouldSaveRemainingDataCorrectly()
        {
            var testData = data with
            {
                General = data.General with
                {
                    injury = new List<InjuryInfo>
                    {
                        new InjuryInfo("Laceration", "Hand", "3", null, null),
                        new InjuryInfo("Hematoma", "Head", "5", null, null)
                    }
                }
            };
            await steps.FillGeneralTabAsync(testData);
            await steps.CreatePage.GetButtonByText("Delete").Nth(1).ClickAsync();
            await steps.ClickCreateIncidentAsync();
            string draftUrl = await steps.GetCurrentUrlAsync();
            await steps.ReloadPageAndNavigateAsync(draftUrl);
            var expectedSavedData = testData with
            {
                General = testData.General with
                {
                    injury = new List<InjuryInfo>
                    {
                        new InjuryInfo("Laceration", "Hand", "3", null, null)
                    }
                }
            };

            await steps.VerifyDataRetainedAsync(expectedSavedData.General);
        }
        #endregion

        #region SBARSummary limit

        /// <summary>
        /// Validates that the SBARSummary field (nvarchar(max)) successfully accepts, transmits, and renders a large block of text after page refresh.
        /// </summary>
        [Test]
        public async Task InjurySection_SBARSummary_LongFormTextInput_ShouldSaveAndRetainCorrectly()
        {
            var minimalGeneral = data.General.GetOnlyRequiredFields();
            var minimalData = data with { General = minimalGeneral };
            await steps.FillGeneralTabAsync(minimalData);
            string longFormText = new string('A', 5000);
            var sbarField = steps.CreatePage.GetFieldByLabel("SBARSummary").First;
            await sbarField.FillAsync(longFormText);
            await steps.ClickCreateIncidentAsync();
            string draftUrl = await steps.GetCurrentUrlAsync();
            await steps.ReloadPageAndNavigateAsync(draftUrl);
            var savedSbarField = steps.CreatePage.GetFieldByLabel("SBARSummary").First;
            await Assertions.Expect(savedSbarField).ToHaveValueAsync(longFormText);
        }
        #endregion
    }
}
