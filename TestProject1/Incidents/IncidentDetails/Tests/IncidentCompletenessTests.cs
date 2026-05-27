
namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    /// <summary>
    /// Encapsulates functional completeness and form validation tests for different layout sheets.
    /// Verifies that dynamic requirement indicator badges ("Red Dots") correctly appear and vanish 
    /// based on input data transitions across multi-tab wizard blocks.
    /// </summary>
    [TestFixture]
    internal class IncidentCompletenessTests : BaseIncidentTests
    {
        /// <summary>
        /// Validates bulk requirement indicators transitions for the General tab 
        /// before submission, after data injection, and following draft persistence.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task GeneralTabCompletenessVerification()
        {
            var tab = "General";

            await steps.ClearGeneralForm();

            // Verify that all mandatory marked input fields display their required completeness indicator badges
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.General, data.General, true);
            // Verify that the parent Tab header element maps with its required incomplete indicator badge
            await steps.VerifyRedDotTab(tab, true);
            // Populate data fields
            await steps.FillGeneralTabAsync(data);

            // Pre-submission check state evaluations
            // Verify that all mandatory marked fields successfully hide their requirement indicator badges
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.General, data.General, false);
            // Verify that the parent Tab header element masks its incomplete indicator badge away
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickCreateIncidentAsync();

            // Post-submission check state evaluations
            // Verify that the parent Tab header element remains clean without incomplete indicator badges
            await steps.VerifyRedDotTab(tab, false);
            // Verify that all mandatory marked input fields remain clean without required completeness indicator badges
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.General, data.General, false);
        }

        /// <summary>
        /// Executes a progressive field-by-field verification on the General tab, 
        /// confirming that filling each explicit input resolves its local required validation status immediately.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task GeneralFieldsCompletenessVerification()
        {
            var tab = "General";

            await steps.ClearGeneralForm();
            await steps.VerifyRedDotField(steps.CreatePage.General, "Date of Incident", true);
            await steps.VerifyRedDotTab(tab, true);

            await steps.VerifyFieldsOneByOneWithFilling(steps.CreatePage.General, data.General);
            await steps.VerifyRedDotTab(tab, false);
        }

        /// <summary>
        /// Validates bulk requirement indicators transitions for the Details tab, 
        /// isolating dynamic behavior of sub-fields triggered conditionally by First Aid toggles.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task DetailsTabCompletenessVerification()
        {
            var tab = "Details";

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);
            string diagnoses = await steps.ClearDetailsForm();
            data = data with
            {
                Details = data.Details with { AllDiagnoses = diagnoses }
            };

            await steps.VerifyRedDotTab(tab, true);

            await steps.SwitchFirstAid(true);
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.Details, data.Details, true);
            await steps.FillDetailsTabAsync(data);

            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.Details, data.Details, false);
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickCreateIncidentAsync();

            await steps.VerifyRedDotTab(tab, false);
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.Details, data.Details, false);
        }





        /// <summary>
        /// Executes a progressive field-by-field verification loop on the Details tab via data dictionary maps, 
        /// confirming that individual form updates dynamically resolve specific input completeness requirements.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task DetailsFieldsCompletenessVerification()
        {
            var tab = "Details";

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);
            string diagnoses = await steps.ClearDetailsForm();
            data = data with
            {
                Details = data.Details with { AllDiagnoses = diagnoses }
            };

            await steps.VerifyRedDotTab(tab, true);

            // All dynamic step loop processing matrix execution happens inside this shared framework method:
            await steps.VerifyFieldsOneByOneWithFilling(steps.CreatePage.Details, data.Details);

            await steps.VerifyRedDotTab(tab, false);
        }

        /// <summary>
        /// Validates bulk requirement indicators transitions for the State tab, 
        /// evaluating tab header badge status responses before and after data persistence.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task StateTabCompletenessVerification()
        {
            var tab = "State";

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);

            // Verify that the parent Tab header element maps with its required incomplete indicator badge
            await steps.VerifyRedDotTab(tab, true);

            // Populate data fields
            await steps.FillStateTabAsync(data);

            // Pre-submission check state evaluations
            // Verify that the parent Tab header element masks its incomplete indicator badge away
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickCreateIncidentAsync();

            // Post-submission check state evaluations
            // Verify that the parent Tab header element remains clean without incomplete indicator badges
            await steps.VerifyRedDotTab(tab, false);
        }

        /// <summary>
        /// Executes state-machine logic validations over the State tab checkbox elements collection, 
        /// verifying rapid indicator appearances and vanishings directly inside state toggling loops.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task StateFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab("State");

            //await steps.VerifyRedDotTab("State", true);

            await steps.VerifyStateTabSpecificLogicAsync();
        }


        /// <summary>
        /// Validates bulk requirement indicator transitions for the Medication tab 
        /// before data entry, after row clearing, and following form saving.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task MedicationTabCompletenessVerification()
        {
            var tab = "Medication";

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);

            // Verify that the parent Tab header element maps with its required incomplete indicator badge
            await steps.VerifyRedDotTab(tab, true);

            // Populate data fields
            await steps.FillMedicationTabAsync(data);

            // Pre-submission check state evaluations
            // Verify that the parent Tab header element masks its incomplete indicator badge away
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickCreateIncidentAsync();


            await steps.ClearMedicationTabAsync();
            await steps.VerifyRedDotTab(tab, true);

            await steps.FillMedicationTabAsync(data);

            await steps.ClickSaveIncidentAsync();

            // Post-submission check state evaluations
            // Verify that the parent Tab header element remains clean without incomplete indicator badges
            await steps.VerifyRedDotTab(tab, false);
        }

        /// <summary>
        /// Executes the full lifecycle and state-machine verification loops for the Medication grid table inputs.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task MedicationFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab("Medication");
            await steps.VerifyMedicationTabFullLifecycleAndIndicatorAsync();
        }

        /// <summary>
        /// Validates bulk requirement indicator transitions for the RN Supervisor Investigation Form tab 
        /// before submission, after data entry, and following draft saving.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task RNInvestigationFormTabCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            var tab = "RN Supervisor Investigation Form";
            await steps.SwitchToTab(tab);

            // Verify that the parent Tab header element maps with its required incomplete indicator badge
            await steps.VerifyRedDotTab(tab, true);

            // Populate data fields
            await steps.FillRNFormTabAsync(data);

            // Pre-submission check state evaluations
            // Verify that the parent Tab header element masks its incomplete indicator badge away
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickCreateIncidentAsync();

            //            await steps.ClickSaveIncidentAsync();

            // Post-submission check state evaluations
            // Verify that the parent Tab header element remains clean without incomplete indicator badges
            await steps.VerifyRedDotTab(tab, false);
        }

        /// <summary>
        /// Validates specific question transitions on the RN Investigation Form tab, 
        /// utilizing inline callback hooks to monitor indicator statuses during processing loops.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task RnInvestigationFormFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.FillDetailsTabAsync(data);
            await steps.SwitchToTab("RN Supervisor Investigation Form");

            await steps.FillRNFormTabWithTabCheckAsync(data);
        }

        /// <summary>
        /// Validates completeness transitions for the Summary tab, checking that the tab header 
        /// remains marked incomplete until required digital signatures are successfully applied.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task SummaryTabCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            var tab = "Summary";
            await steps.SwitchToTab(tab);

            // Verify that the parent Tab header element maps with its required incomplete indicator badge
            await steps.VerifyRedDotTab(tab, true);

            // Populate data fields
            await steps.FillSummaryTabAsync(data);

            // Pre-submission check state evaluations
            // Verify that the tab remains marked incomplete because signatures are still missing
            await steps.VerifyRedDotTab(tab, true);
            await steps.ClickSaveIncidentAsync();

            // After initial baseline persistence checks
            await steps.VerifyRedDotTab(tab, true);

            // Commit digital signatures sign-off workflows
            await steps.SignSummaryAndVerifyAsync();
            await steps.ClickSaveIncidentAsync(true);

            // Post-submission check state evaluations
            // Verify that the parent Tab header element successfully masks its incomplete indicator badge away
            await steps.VerifyRedDotTab(tab, false);
        }

        /// <summary>
        /// Executes a progressive field-by-field verification loop on the Summary tab via data dictionary maps, 
        /// confirming that individual form updates dynamically resolve specific input completeness requirements.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task SummaryFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab("Summary");

            await steps.VerifyFieldsOneByOneWithFilling(steps.CreatePage.Summary, data.Summary);
        }

        /// <summary>
        /// Executes an incremental file upload evaluation loop on the Attachments tab, verifying that 
        /// the tab completeness indicator badge reacts dynamically once data threshold boundaries are crossed.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task AttachmentsTabCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // Verify that the parent Tab header element maps with its required incomplete indicator badge before upload sequences initialize
            await steps.VerifyRedDotTab(tab, true);

            int max = 10;
            int check = 1; // Operational boundary line determining when the indicator badge should vanish

            for (int i = 0; i < max; i++)
            {
                // Extract category strings dynamically by tracking loop iteration indexes against static collection records
                string currentCategory = AttachmentsTab.AttachmentCategories[i];
                string? note = currentCategory.Equals("Other", StringComparison.OrdinalIgnoreCase)
                    ? "Test internal note for Other category"
                    : null;

                // Pass the dynamically resolved category name forward into the file streaming upload processor pipeline
                await steps.UploadAttachmentTabAsync(currentCategory, note, fileNameString: "test_1page.pdf", toScreenShot: true);
                await Page.WaitForTimeoutAsync(1000);

                // Evaluate the current state of the tab header completeness indicator badge
                // If uploaded item counts scale lower than 'check' thresholds (first lines execution bounds), the indicator badge stays visible
                if (i < check - 1)
                {
                    await steps.VerifyRedDotTab(tab, true);
                }
                else
                {
                    // As soon as the designated initial file uploading transactions terminate successfully
                    await steps.VerifyRedDotTab(tab, false);
                }
            }
        }

        /// <summary>
        /// Verifies that uploading a single multi-page file and mapping required category layouts 
        /// successfully satisfies and clears the Attachments tab completeness requirement in one action.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task AttachmentsTabSingleMultyPageFileCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // Verify that the parent Tab header element maps with its required incomplete indicator badge
            await steps.VerifyRedDotTab(tab, true);

            // Populate multi-page attachment data parameters
            await steps.UploadAttachmentTabAsync(AttachmentsTab.AttachmentCategories, fileNameString: "test_10pages.pdf", toScreenShot: true);
            await steps.VerifyRedDotTab(tab, false);
        }
    }
}


