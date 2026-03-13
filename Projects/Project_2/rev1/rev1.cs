using System;

namespace QuestEngine.Interfaces
{
    // команда игрока go, look итд
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        void Execute(GameContext context);
    }
    // проверка условия
    public interface ICondition
    {
        string Description { get; }
        bool Check(GameState state);
    }

    // эффект
    public interface IEffect
    {
        string Description { get; }
        void Apply(GameState state);
    }

    // взаимодействие
    public interface IInteractable
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        bool CanInteract(GameState state);
        void Interact(GameState state);
    }

    public interface IGameEvent
    {
        string Description { get; }
        bool IsOneTime { get; }
        bool IsTriggered { get; }
        bool ShouldTrigger(GameState state);
        void Trigger(GameState state);
    }

    public interface IQuest
    {
        string Id { get; }
        string Title { get; }
        string Description { get; }
        bool IsCompleted { get; }
        void Update(GameState state);
    }
}

public class GameContext
{
    public GameState State { get; }   //  состояние игры 
    public Game Game { get; }         //  главная игра
    public string[] Args { get; }     //  аргументы команды 
}

public class GameState
{
    // Здоровье
    public int Health { get; private set; }
    public int MaxHealth { get; } = 100;
    
    // Инвентарь
    public List<string> Inventory { get; } = new List<string>();
    
    //  включённые события
    public Dictionary<string, bool> Flags { get; } = new Dictionary<string, bool>();
    
    // Журнал событий (что происходило в игре)
    public List<string> Log { get; } = new List<string>();
    
    // Счётчик ходов
    public int Turn { get; private set; }
    
    // Где сейчас игрок
    public Location CurrentLocation { get; set; }
    
    // Жив ли игрок
    public bool IsAlive => Health > 0;
    public bool IsGameOver { get; private set; }
}
