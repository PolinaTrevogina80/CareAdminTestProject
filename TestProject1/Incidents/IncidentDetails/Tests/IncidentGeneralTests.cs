using Log = CareAdminTestProject.Common.TestLog;


namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{

    [TestFixture]
    public class IncidentGeneralTests : BaseIncidentTests
    {
        #region 1. Asterisk Validation (Minimum for creation)

        [Test]
        [Description("Verify that the 'Create' button is disabled until all asterisk-marked fields are filled.")]
        public async Task Test_CreateButtonLock_UntilFieldsFilled()
        {
            // Form is freshly opened and empty -> Button must be disabled
            await steps.VerifyCreateButtonStateAsync(shouldBeEnabled: false);
        }

        [Test]
        [Description("Sequentially fill the asterisk-marked fields -> verify that the 'Create' button becomes active.")]
        public async Task Test_MinimumSetCompletion_ActivatesCreateButton()
        {
            // Обрезаем данные до минимально обязательных с помощью твоего метода
            var minimalData = data with { General = data.General.GetOnlyRequiredFields() };

            // Form is filled with  -> Button must be disabled
            await steps.FillGeneralTabAsync(minimalData);
            await steps.VerifyCreateButtonStateAsync(shouldBeEnabled: true);
        }

        [Test]
        [Description("Verify that the Kendo UI calendar and time picker strictly prevent selecting future dates and times via UI constraints.")]
        public async Task Test_DateValidation_PreventsFutureDateTime()
        {
            Log.Information("Starting test: UI-Driven Future Date and Time Validation");

            // 1. Проверяем, что в календаре нельзя кликнуть на "завтра"
            await steps.VerifyTomorrowIsDisabledInCalendarAsync();

            // 2. Проверяем, что в пикере времени заблокировано будущее относительно текущего момента
            await steps.VerifyFutureTimeIsDisabledInPickerAsync();

            Log.Information("All future-time UI constraints successfully verified.");
        }

        #endregion

        #region 3. Auto-population and Dependencies

        [Test]
        [Description("Verify that Room, Bed, and Unit fields are automatically pulled from the selected resident's profile.")]
        public async Task Test_ResidentData_AutoPopulation()
        {
            Log.Debug("Starting test: Resident Data Auto-population");

            // 1. Создаем объект данных, где ОСТАВЛЯЕМ только предзаполненные поля из дефолтного data
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

            // 2. Вызываем готовый метод верификации
            await steps.VerifyDataRetainedAsync(expectedAutopopulated);

            Log.Information("Resident profile auto-population fields constraints successfully verified.");
        }

        [Test]
        [Description("Verify that staff dropdowns correctly filter current employees based on their employment status (termDate).")]
        public async Task Test_StaffLists_DisplayCorrectFacilityEmployeesCount()
        {
            Log.Information("Starting test: Staff Lists Validation via API Counters");

            // Запускаем наш умный метод верификации каунтеров
            await steps.VerifyStaffDropdownsCountWithApiAsync();
        }

        #endregion
    }
}
