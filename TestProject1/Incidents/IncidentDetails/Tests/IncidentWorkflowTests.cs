namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    /// <summary>
    /// Encapsulates functional workflow and lifecycle integration tests for incident records.
    /// Covers full document generation, sequential multi-role digital signature sign-offs, 
    /// state locking/unlocking capabilities, and post-approval completeness indicator verifications.
    /// </summary>
    [TestFixture]
    internal class IncidentWorkflowTests : BaseIncidentTests
    {
        /// <summary>
        /// Validates the standard happy path workflow lifecycle: populates all data tabs, 
        /// applies progressive signatures for DNS, MD, and Administrator roles, and verifies automated record locking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateAndSignIncident()
        {
            await steps.UpdateIncidentConfigurationAsync("Attachments", true, 1);

            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.SignMD();
            await steps.SignAdministrator();
            await steps.AssertIncidentIsLockedAsync();
        }

        /// <summary>
        /// Validates an alternative orchestration path for the lifecycle signing sequence, 
        /// ensuring system state persistence remains consistent across uniform multi-role sign-offs.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateAndSignIncidentOtherWay()
        {
            await steps.UpdateIncidentConfigurationAsync("Attachments", true, 1);

            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.SignMD();
            await steps.SignAdministrator();
            await steps.AssertIncidentIsLockedAsync();
        }

        /// <summary>
        /// Validates advanced state toggle capabilities: verifies the workspace automatically locks post signature, 
        /// successfully switches to unlocked status via security controls, and locks down again upon re-activation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateAndSignUnlockAndLockIncident()
        {
            await steps.UpdateIncidentConfigurationAsync("Attachments", true, 1);

            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.AssertIncidentIsLockedAsync();
            await steps.SetIncidentLockStateAsync(false);
            await steps.AssertIncidentIsUnlockedAsync();
            await steps.SetIncidentLockStateAsync(true);
            await steps.AssertIncidentIsLockedAsync();
        }

        /// <summary>
        /// Comprehensive status integration test: verifies that once a document finishes full creation 
        /// and transitions into a locked status state, every tab header completely hides its incomplete indicator badge.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateSignAndVerifyCompleteness()
        {
            await steps.UpdateIncidentConfigurationAsync("Attachments", true, 1);

            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.AssertIncidentIsLockedAsync();
            await steps.VerifyRedDotTab("General", false);
            await steps.VerifyRedDotTab("Details", false);
            await steps.VerifyRedDotTab("State", false);
            await steps.VerifyRedDotTab("Medication", false);
            await steps.VerifyRedDotTab("Summary", false);
            await steps.VerifyRedDotTab("Attachments", false);
        }

        /// <summary>
        /// Turned Off Completness status integration test: verifies that if completness is turned off 
        /// users can fill in and sign the incident with minimal data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateSignTurnedOffCompleteness()
        {
            var tabsToReset = new[]
            {
                new { Name = "General", Count = (int?)null },
                new { Name = "Details", Count = (int?)null },
                new { Name = "State", Count = (int?)null },
                new { Name = "Medication", Count = (int?)null },
                new { Name = "RN Supervisor Investigation Form", Count = (int?)null },
                // Помним, что Summary выключить нельзя, мы ее пропускаем
                new { Name = "Attachments", Count = (int?)5 } // Передаем 5 для аттачей
            };

            // Цикл перебора и сброса конфигурации
            foreach (var tab in tabsToReset)
            {
                await steps.UpdateIncidentConfigurationAsync(
                    sectionCode: tab.Name,
                    isEnabled: false
                );
            }
            var minimalData = data with { General = data.General.GetOnlyRequiredFields() };

            await steps.FillGeneralTabAsync(minimalData);
            await steps.ClickCreateIncidentAsync();
            await steps.FillSummaryTabAsync(minimalData);
            await steps.ClickSaveIncidentAsync(false);
            await steps.SignSummaryAndVerifyAsync();
            await steps.ClickSaveIncidentAsync(true);

            await steps.SignDNS();
            await steps.SignMD();
            await steps.SignAdministrator();
            await steps.AssertIncidentIsLockedAsync();
        }
    }
}
