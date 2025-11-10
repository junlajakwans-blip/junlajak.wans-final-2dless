using System.Collections.Generic;
using UnityEngine;

public interface ICareerSwitchable
{
    DuckCareerData CurrentCareer { get; }

    // Switch to a new career when use card
    void SwitchCareer(DuckCareerData newCareer);

    // list of careers that can be switch to
    List<DuckCareer> GetAvailableCareers();

    // Callback when career has been changed
    void OnCareerChanged(DuckCareerData newCareer);
}
