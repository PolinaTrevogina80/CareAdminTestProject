using CareAdminTestProject.Incidents.IncidentDetails.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using static IncidentCreatePage;
using static System.Net.Mime.MediaTypeNames;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    internal class IncidentWorkflowTests : BaseIncidentTests
    {

        [Test]
        public async Task CreateAndSignIncident()
        {
            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.SignMD();
            await steps.SignAdministrator();
            await steps.AssertIncidentIsLockedAsync();
        }

        [Test]
        public async Task CreateAndSignIncidentOtherWay()
        {
            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.SignMD();
            await steps.SignAdministrator();
            await steps.AssertIncidentIsLockedAsync();
        }

        [Test]
        public async Task CreateAndSignUnlockAndLockIncident()
        {
            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.AssertIncidentIsLockedAsync();
            await steps.SetIncidentLockStateAsync(false);
            await steps.AssertIncidentIsUnlockedAsync();
            await steps.SetIncidentLockStateAsync(true);
            await steps.AssertIncidentIsLockedAsync();

        }

        [Test]
        public async Task CreateSignAndVerifyCompleteness()
        {
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
    }
}
