using System.Collections.Generic;
using Backend.Object.Management;

namespace Backend.Object.UI
{
    public class UnitPresenter : UIPresenter<UnitView>
    {
        public IReadOnlyList<UserUnitData> GetUnitData() => UserData.OwnedUnitIds;
    }
}
