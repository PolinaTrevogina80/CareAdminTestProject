using CareAdminTestProject.Incidents.IncidentDetails.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using static IncidentCreatePage;
using static System.Net.Mime.MediaTypeNames;

[TestFixture]
public class IncidentTests : BaseIncidentTests
{

    [Test]
    public async Task CreateIncident_StartWithResidentSelection()
    {
        await steps.FillGeneralTabAsync(data);
        await steps.ClickCreateIncidentAsync();

        await steps.FillDetailsTabAsync(data);
        await steps.FillStateTabAsync(data);
        await steps.FillMedicationTabAsync(data);
        await steps.FillRNFormTabAsync(data);
        await steps.FillSummaryTabAsync(data);

        // 6. Сохранение и подписание
        await steps.ClickSaveIncidentAsync();
        await steps.SignSummaryAndVerifyAsync();
        await steps.ClickSaveIncidentAsync(true);

        // 7. добавление аттачей
        await steps.UploadAttachmentTabAsync("Other", "This is a test note");
        //await steps.SaveIncidentAsync();

    }


}