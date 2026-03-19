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
using System;

namespace QuestEngine.Core
{
    // Хранит всё состояние игры
    public class GameState
    {
        public int Health { get; private set; }
        public int MaxHealth { get; } = 100;
        
        public System.Collections.Generic.List<string> Inventory { get; } 
            = new System.Collections.Generic.List<string>();
        
        public System.Collections.Generic.Dictionary<string, bool> Flags { get; } 
            = new System.Collections.Generic.Dictionary<string, bool>();
        
        public System.Collections.Generic.List<string> Log { get; } 
            = new System.Collections.Generic.List<string>();
        
        public int Turn { get; private set; }
        public Location CurrentLocation { get; set; }
        public bool IsAlive => Health > 0;
        public bool IsGameOver { get; private set; }

        public GameState()
        {
            Health = MaxHealth;
            Turn = 0;
            IsGameOver = false;
        }

        // Предметы
        public void AddItem(string id)
        {
            if (!Inventory.Contains(id))
                Inventory.Add(id);
        }

        public void RemoveItem(string id)
        {
            Inventory.Remove(id);
        }

        public bool HasItem(string id)
        {
            return Inventory.Contains(id);
        }

        // Флаги
        public void SetFlag(string key, bool value)
        {
            Flags[key] = value;
        }

        public bool GetFlag(string key)
        {
            if (Flags.ContainsKey(key))
                return Flags[key];
            return false;
        }

        // Здоровье
        public void TakeDamage(int amount)
        {
            Health = Health - amount;
            if (Health < 0)
                Health = 0;
            if (Health == 0)
                IsGameOver = true;
        }

        public void Heal(int amount)
        {
            Health = Health + amount;
            if (Health > MaxHealth)
                Health = MaxHealth;
        }

        // Журнал
        public void LogMessage(string message)
        {
            Log.Add("[Ход " + Turn + "] " + message);
        }

        // Ход
        public void NextTurn()
        {
            Turn = Turn + 1;
        }

        // Конец игры
        public void EndGame()
        {
            IsGameOver = true;
        }
    }

    // Контекст для команд
    public class GameContext
    {
        public GameState State { get; }
        public Game Game { get; }
        public string[] Args { get; }

        public GameContext(Game game, GameState state, string[] args)
        {
            Game = game;
            State = state;
            Args = args;
        }
    }

    // Локация
    public class Location
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        
        public System.Collections.Generic.Dictionary<string, Location> Exits { get; }
            = new System.Collections.Generic.Dictionary<string, Location>();
        
        public System.Collections.Generic.List<Interfaces.IInteractable> Objects { get; }
            = new System.Collections.Generic.List<Interfaces.IInteractable>();
        
        public System.Collections.Generic.List<Interfaces.IGameEvent> Events { get; }
            = new System.Collections.Generic.List<Interfaces.IGameEvent>();

        public Location(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public void AddExit(string direction, Location location)
        {
            Exits[direction] = location;
        }

        public void AddObject(Interfaces.IInteractable obj)
        {
            Objects.Add(obj);
        }

        public void AddEvent(Interfaces.IGameEvent evt)
        {
            Events.Add(evt);
        }

        public Interfaces.IInteractable FindObject(string id)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].Id == id)
                    return Objects[i];
            }
            return null;
        }
    }

    // Главный класс игры
    public class Game
    {
        private GameState _state;
        private System.Collections.Generic.Dictionary<string, Interfaces.ICommand> _commands;
        private System.Collections.Generic.List<Interfaces.IQuest> _quests;
        private Location _startLocation;

        public Game()
        {
            _state = new GameState();
            _commands = new System.Collections.Generic.Dictionary<string, Interfaces.ICommand>();
            _quests = new System.Collections.Generic.List<Interfaces.IQuest>();
        }

        public void SetStartLocation(Location location)
        {
            _startLocation = location;
        }

        public void RegisterCommand(Interfaces.ICommand command)
        {
            _commands[command.Name.ToLower()] = command;
        }

        public void RegisterQuest(Interfaces.IQuest quest)
        {
            _quests.Add(quest);
        }

        public void Start()
        {
            if (_startLocation == null)
            {
                throw new InvalidOperationException("Нет стартовой локации");
            }
            _state.CurrentLocation = _startLocation;
            _state.LogMessage("Игра началась!");
        }

        public void ProcessCommand(string input)
        {
            string[] parts = input.Trim().Split(' ');
            if (parts.Length == 0)
                return;

            string cmdName = parts[0].ToLower();
            
            // Копируем аргументы
            string[] args = new string[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
                args[i - 1] = parts[i];
            }

            if (_commands.ContainsKey(cmdName))
            {
                Interfaces.ICommand command = _commands[cmdName];
                GameContext context = new GameContext(this, _state, args);
                command.Execute(context);

                // Обновляем квесты
                for (int i = 0; i < _quests.Count; i++)
                {
                    _quests[i].Update(_state);
                }

                // Следующий ход
                _state.NextTurn();

                // Проверяем события
                if (_state.CurrentLocation != null)
                {
                    for (int i = 0; i < _state.CurrentLocation.Events.Count; i++)
                    {
                        Interfaces.IGameEvent evt = _state.CurrentLocation.Events[i];
                        if (evt.ShouldTrigger(_state))
                        {
                            evt.Trigger(_state);
                        }
                    }
                }
            }
            else
            {
                _state.LogMessage("Неизвестная команда: " + cmdName);
            }
        }

        public bool IsRunning => !IsGameOver && IsAlive;
        public bool IsGameOver => _state.IsGameOver;
        public bool IsAlive => _state.IsAlive;
        public GameState State => _state;
    }
}
using System;

namespace QuestEngine.Base
{
    // Базовая команда
    public abstract class CommandBase : Interfaces.ICommand
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract void Execute(Core.GameContext context);
    }

    // Базовое условие
    public abstract class ConditionBase : Interfaces.ICondition
    {
        public abstract string Description { get; }
        public abstract bool Check(Core.GameState state);
    }

    // Базовый эффект
    public abstract class EffectBase : Interfaces.IEffect
    {
        public abstract string Description { get; }
        public abstract void Apply(Core.GameState state);
    }

    // Базовый объект
    public abstract class InteractableBase : Interfaces.IInteractable
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool CanInteract(Core.GameState state);
        public abstract void Interact(Core.GameState state);
    }

    // Базовое событие
    public abstract class GameEventBase : Interfaces.IGameEvent
    {
        public abstract string Description { get; }
        public abstract bool IsOneTime { get; }
        public bool IsTriggered { get; protected set; }
        public abstract bool ShouldTrigger(Core.GameState state);
        public abstract void Trigger(Core.GameState state);
    }

    // Базовый квест
    public abstract class QuestBase : Interfaces.IQuest
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string Description { get; }
        public bool IsCompleted { get; protected set; }
        public abstract void Update(Core.GameState state);
    }
}
