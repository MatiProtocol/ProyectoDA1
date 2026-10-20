namespace Auxiliaries;

public class Reactivity
{
   public event Action? OnChange;
   public event Action? OnChangeTaskGraph;
   private static Reactivity? _instance = null;

   public static Reactivity GetInstance()
   {
      if (_instance == null) _instance = new Reactivity();
      return _instance;
   }

   private Reactivity() { }

   public void Invoke()
   {
      OnChange?.Invoke();
   }

   public void InvokeTaskGraph()
   {
      OnChangeTaskGraph?.Invoke();
   }
}
