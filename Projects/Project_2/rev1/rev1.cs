using System;
using QuestEngine.Core;
using QuestEngine.Commands;
using QuestEngine.Quests;
using QuestEngine.Conditions;
using QuestEngine.Effects;
using QuestEngine.Objects;
using QuestEngine.Events;
using QuestEngine.Interfaces;

// ============================================
// ИНТЕРФЕЙСЫ
// ============================================
namespace QuestEngine.Interfaces
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        void Execute(Core.GameContext context);
    }

    public interface ICondition
    {
        string Description { get; }
        bool Check(Core.GameState state);
    }

    public interface IEffect
    {
        string Description { get; }
        void Apply(Core.GameState state);
    }

    public interface IInteractable
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        bool CanInteract(Core.GameState state);
        void Interact(Core.GameState state);
    }

    public interface IGameEvent
    {
        string Description { get; }
        bool IsOneTime { get; }
        bool IsTriggered { get; }
        bool ShouldTrigger(Core.GameState state);
        void Trigger(Core.GameState state);
    }

    public interface IQuest
    {
        string Id { get; }
        string Title { get; }
        string Description { get; }
        bool IsCompleted { get; }
        void Update(Core.GameState state);
    }
}

// ============================================
// ЯДРО ИГРЫ (Core)
// ============================================
namespace QuestEngine.Core
{
    public class GameState
    {
        public int Health { get; private set; }
        public int MaxHealth { get; } = 100;
        public System.Collections.Generic.List<string> Inventory { get; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.Dictionary<string, bool> Flags { get; } = new System.Collections.Generic.Dictionary<string, bool>();
        public System.Collections.Generic.List<string> Log { get; } = new System.Collections.Generic.List<string>();
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

        public void LogMessage(string message)
        {
            Log.Add("[Ход " + Turn + "] " + message);
        }

        public void NextTurn()
        {
            Turn = Turn + 1;
        }

        public void EndGame()
        {
            IsGameOver = true;
        }
    }

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

    public class Location
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public System.Collections.Generic.Dictionary<string, Location> Exits { get; } = new System.Collections.Generic.Dictionary<string, Location>();
        public System.Collections.Generic.List<Interfaces.IInteractable> Objects { get; } = new System.Collections.Generic.List<Interfaces.IInteractable>();
        public System.Collections.Generic.List<Interfaces.IGameEvent> Events { get; } = new System.Collections.Generic.List<Interfaces.IGameEvent>();

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
                if (Objects[i].Id == id || Objects[i].Name == id)
                    return Objects[i];
            }
            return null;
        }
    }

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
            _state.LogMessage("Игра началась! Добро пожаловать в Сектор-7.");
        }

        public void ProcessCommand(string input)
        {
            string[] parts = input.Trim().Split(' ');
            if (parts.Length == 0)
                return;

            string cmdName = parts[0].ToLower();
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

                for (int i = 0; i < _quests.Count; i++)
                {
                    _quests[i].Update(_state);
                }

                _state.NextTurn();

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

// ============================================
// АБСТРАКТНЫЕ КЛАССЫ (Base)
// ============================================
namespace QuestEngine.Base
{
    public abstract class CommandBase : Interfaces.ICommand
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract void Execute(Core.GameContext context);
    }

    public abstract class ConditionBase : Interfaces.ICondition
    {
        public abstract string Description { get; }
        public abstract bool Check(Core.GameState state);
    }

    public abstract class EffectBase : Interfaces.IEffect
    {
        public abstract string Description { get; }
        public abstract void Apply(Core.GameState state);
    }

    public abstract class InteractableBase : Interfaces.IInteractable
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool CanInteract(Core.GameState state);
        public abstract void Interact(Core.GameState state);
    }

    public abstract class GameEventBase : Interfaces.IGameEvent
    {
        public abstract string Description { get; }
        public abstract bool IsOneTime { get; }
        public bool IsTriggered { get; protected set; }
        public abstract bool ShouldTrigger(Core.GameState state);
        public abstract void Trigger(Core.GameState state);
    }

    public abstract class QuestBase : Interfaces.IQuest
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string Description { get; }
        public bool IsCompleted { get; protected set; }
        public abstract void Update(Core.GameState state);
    }
}

// ============================================
// КОМАНДЫ
// ============================================
namespace QuestEngine.Commands
{
    public class HelpCommand : Base.CommandBase
    {
        public override string Name => "help";
        public override string Description => "Показать список команд";
        public override void Execute(Core.GameContext context)
        {
            context.State.LogMessage("=== КОМАНДЫ ===");
            context.State.LogMessage("help - список команд");
            context.State.LogMessage("look - осмотреться");
            context.State.LogMessage("go <направление> - перейти");
            context.State.LogMessage("interact <id> - взаимодействовать");
            context.State.LogMessage("inv - инвентарь");
            context.State.LogMessage("status - статус игрока");
        }
    }

    public class LookCommand : Base.CommandBase
    {
        public override string Name => "look";
        public override string Description => "Осмотреть локацию";
        public override void Execute(Core.GameContext context)
        {
            Core.Location loc = context.State.CurrentLocation;
            if (loc == null)
            {
                context.State.LogMessage("Вы нигде.");
                return;
            }
            context.State.LogMessage("=== " + loc.Name + " ===");
            context.State.LogMessage(loc.Description);
            if (loc.Objects.Count > 0)
            {
                string objs = "";
                for (int i = 0; i < loc.Objects.Count; i++)
                {
                    if (i > 0) objs = objs + ", ";
                    objs = objs + loc.Objects[i].Name;
                }
                context.State.LogMessage("Объекты: " + objs);
            }
            if (loc.Exits.Count > 0)
            {
                string exits = "";
                foreach (System.Collections.Generic.KeyValuePair<string, Core.Location> pair in loc.Exits)
                {
                    if (exits != "") exits = exits + ", ";
                    exits = exits + pair.Key;
                }
                context.State.LogMessage("Выходы: " + exits);
            }
        }
    }

    public class GoCommand : Base.CommandBase
    {
        public override string Name => "go";
        public override string Description => "Перейти (go <направление>)";
        public override void Execute(Core.GameContext context)
        {
            if (context.Args.Length == 0)
            {
                context.State.LogMessage("Куда идти? go north, go south...");
                return;
            }
            string dir = context.Args[0].ToLower();
            Core.Location loc = context.State.CurrentLocation;
            if (loc.Exits.ContainsKey(dir))
            {
                context.State.CurrentLocation = loc.Exits[dir];
                context.State.LogMessage("Вы перешли в: " + context.State.CurrentLocation.Name);
            }
            else
            {
                context.State.LogMessage("Туда нельзя пройти.");
            }
        }
    }

    public class InteractCommand : Base.CommandBase
    {
        public override string Name => "interact";
        public override string Description => "Взаимодействовать (interact <id>)";
        public override void Execute(Core.GameContext context)
        {
            if (context.Args.Length == 0)
            {
                context.State.LogMessage("С чем взаимодействовать? interact <id>");
                return;
            }
            string objId = context.Args[0];
            Core.Location loc = context.State.CurrentLocation;
            Interfaces.IInteractable obj = loc.FindObject(objId);
            if (obj == null)
            {
                context.State.LogMessage("Объект не найден.");
                return;
            }
            if (obj.CanInteract(context.State))
            {
                obj.Interact(context.State);
            }
            else
            {
                context.State.LogMessage("Нельзя взаимодействовать сейчас.");
            }
        }
    }

    public class InventoryCommand : Base.CommandBase
    {
        public override string Name => "inv";
        public override string Description => "Показать инвентарь";
        public override void Execute(Core.GameContext context)
        {
            System.Collections.Generic.List<string> inv = context.State.Inventory;
            if (inv.Count == 0)
            {
                context.State.LogMessage("Инвентарь пуст.");
            }
            else
            {
                string items = "";
                for (int i = 0; i < inv.Count; i++)
                {
                    if (i > 0) items = items + ", ";
                    items = items + inv[i];
                }
                context.State.LogMessage("Инвентарь: " + items);
            }
        }
    }

    public class StatusCommand : Base.CommandBase
    {
        public override string Name => "status";
        public override string Description => "Статус игрока";
        public override void Execute(Core.GameContext context)
        {
            context.State.LogMessage("Здоровье: " + context.State.Health + "/" + context.State.MaxHealth);
            context.State.LogMessage("Ход: " + context.State.Turn);
        }
    }
}

// ============================================
// УСЛОВИЯ
// ============================================
namespace QuestEngine.Conditions
{
    public class HasItemCondition : Base.ConditionBase
    {
        private string _itemId;
        public override string Description => "Есть предмет: " + _itemId;
        public HasItemCondition(string itemId) { _itemId = itemId; }
        public override bool Check(Core.GameState state) { return state.HasItem(_itemId); }
    }

    public class FlagCondition : Base.ConditionBase
    {
        private string _flag;
        private bool _value;
        public override string Description => "Флаг " + _flag + " = " + _value;
        public FlagCondition(string flag, bool value = true) { _flag = flag; _value = value; }
        public override bool Check(Core.GameState state) { return state.GetFlag(_flag) == _value; }
    }

    public class HealthCondition : Base.ConditionBase
    {
        private int _minHealth;
        public override string Description => "Здоровье >= " + _minHealth;
        public HealthCondition(int minHealth) { _minHealth = minHealth; }
        public override bool Check(Core.GameState state) { return state.Health >= _minHealth; }
    }

    public class AndCondition : Base.ConditionBase
    {
        private System.Collections.Generic.List<Interfaces.ICondition> _conditions;
        public override string Description => "Все условия истинны";
        public AndCondition(params Interfaces.ICondition[] conditions)
        {
            _conditions = new System.Collections.Generic.List<Interfaces.ICondition>();
            for (int i = 0; i < conditions.Length; i++) { _conditions.Add(conditions[i]); }
        }
        public override bool Check(Core.GameState state)
        {
            for (int i = 0; i < _conditions.Count; i++)
            {
                if (!_conditions[i].Check(state)) return false;
            }
            return true;
        }
    }

    public class OrCondition : Base.ConditionBase
    {
        private System.Collections.Generic.List<Interfaces.ICondition> _conditions;
        public override string Description => "Хотя бы одно условие истинно";
        public OrCondition(params Interfaces.ICondition[] conditions)
        {
            _conditions = new System.Collections.Generic.List<Interfaces.ICondition>();
            for (int i = 0; i < conditions.Length; i++) { _conditions.Add(conditions[i]); }
        }
        public override bool Check(Core.GameState state)
        {
            for (int i = 0; i < _conditions.Count; i++)
            {
                if (_conditions[i].Check(state)) return true;
            }
            return false;
        }
    }

    public class NotCondition : Base.ConditionBase
    {
        private Interfaces.ICondition _condition;
        public override string Description => "Отрицание условия";
        public NotCondition(Interfaces.ICondition condition) { _condition = condition; }
        public override bool Check(Core.GameState state) { return !_condition.Check(state); }
    }
}

// ============================================
// ЭФФЕКТЫ
// ============================================
namespace QuestEngine.Effects
{
    public class AddItemEffect : Base.EffectBase
    {
        private string _itemId;
        public override string Description => "Добавить: " + _itemId;
        public AddItemEffect(string itemId) { _itemId = itemId; }
        public override void Apply(Core.GameState state) { state.AddItem(_itemId); }
    }

    public class RemoveItemEffect : Base.EffectBase
    {
        private string _itemId;
        public override string Description => "Удалить: " + _itemId;
        public RemoveItemEffect(string itemId) { _itemId = itemId; }
        public override void Apply(Core.GameState state) { state.RemoveItem(_itemId); }
    }

    public class SetFlagEffect : Base.EffectBase
    {
        private string _flag;
        private bool _value;
        public override string Description => "Флаг " + _flag + " = " + _value;
        public SetFlagEffect(string flag, bool value = true) { _flag = flag; _value = value; }
        public override void Apply(Core.GameState state) { state.SetFlag(_flag, _value); }
    }

    public class DamageEffect : Base.EffectBase
    {
        private int _amount;
        public override string Description => "Урон: " + _amount;
        public DamageEffect(int amount) { _amount = amount; }
        public override void Apply(Core.GameState state) { state.TakeDamage(_amount); }
    }

    public class HealEffect : Base.EffectBase
    {
        private int _amount;
        public override string Description => "Лечение: " + _amount;
        public HealEffect(int amount) { _amount = amount; }
        public override void Apply(Core.GameState state) { state.Heal(_amount); }
    }

    public class LogEffect : Base.EffectBase
    {
        private string _message;
        public override string Description => "Сообщение: " + _message;
        public LogEffect(string message) { _message = message; }
        public override void Apply(Core.GameState state) { state.LogMessage(_message); }
    }

    public class AddExitEffect : Base.EffectBase
    {
        private string _fromLocId;
        private string _direction;
        private string _toLocId;
        public override string Description => "Открыть выход " + _direction;
        public AddExitEffect(string fromLocId, string direction, string toLocId)
        { _fromLocId = fromLocId; _direction = direction; _toLocId = toLocId; }
        public override void Apply(Core.GameState state)
        {
            state.LogMessage("Открыт новый выход: " + _direction);
        }
    }

    public class ChangeLocationEffect : Base.EffectBase
    {
        private Core.Location _location;
        public override string Description => "Переместить в: " + _location.Name;
        public ChangeLocationEffect(Core.Location location) { _location = location; }
        public override void Apply(Core.GameState state)
        {
            state.CurrentLocation = _location;
            state.LogMessage("Вы перемещены в: " + _location.Name);
        }
    }
}

// ============================================
// ОБЪЕКТЫ
// ============================================
namespace QuestEngine.Objects
{
    public class Chest : Base.InteractableBase
    {
        private string _itemId;
        private bool _opened = false;
        public override string Id { get; }
        public override string Name { get; }
        public override string Description { get; }
        public Chest(string id, string name, string itemId)
        {
            Id = id; Name = name;
            Description = "Старый " + name + ". Внутри что-то есть.";
            _itemId = itemId;
        }
        public override bool CanInteract(Core.GameState state) { return !_opened; }
        public override void Interact(Core.GameState state)
        {
            _opened = true;
            state.AddItem(_itemId);
            state.LogMessage("Вы открыли " + Name + " и нашли: " + _itemId);
        }
    }

    public class Door : Base.InteractableBase
    {
        private Interfaces.ICondition _unlockCondition;
        private bool _isOpen = false;
        public override string Id { get; }
        public override string Name { get; }
        public override string Description { get; }
        public Door(string id, string name, Interfaces.ICondition unlockCondition = null)
        {
            Id = id; Name = name; _unlockCondition = unlockCondition;
            Description = "Запертая " + name + ".";
        }
        public override bool CanInteract(Core.GameState state) { return !_isOpen; }
        public override void Interact(Core.GameState state)
        {
            if (_unlockCondition == null || _unlockCondition.Check(state))
            {
                _isOpen = true;
                state.LogMessage(Name + " открыта!");
            }
            else
            {
                state.LogMessage(Name + " заперта. Нужно условие.");
            }
        }
    }

    public class NPC : Base.InteractableBase
    {
        private System.Collections.Generic.List<Interfaces.IEffect> _effects;
        public override string Id { get; }
        public override string Name { get; }
        public override string Description { get; }
        public NPC(string id, string name, string description, System.Collections.Generic.List<Interfaces.IEffect> effects)
        {
            Id = id; Name = name; Description = description; _effects = effects;
        }
        public override bool CanInteract(Core.GameState state) { return true; }
        public override void Interact(Core.GameState state)
        {
            for (int i = 0; i < _effects.Count; i++) { _effects[i].Apply(state); }
        }
    }

    public class Trap : Base.InteractableBase
    {
        private int _damage;
        private bool _triggered = false;
        public override string Id { get; }
        public override string Name => "Ловушка";
        public override string Description => "Подозрительный механизм.";
        public Trap(string id, int damage) { Id = id; _damage = damage; }
        public override bool CanInteract(Core.GameState state) { return !_triggered; }
        public override void Interact(Core.GameState state)
        {
            _triggered = true;
            state.TakeDamage(_damage);
            state.LogMessage("Ловушка! Урон: " + _damage);
        }
    }
}

// ============================================
// СОБЫТИЯ
// ============================================
namespace QuestEngine.Events
{
    public class OnEnterLocationEvent : Base.GameEventBase
    {
        private Interfaces.ICondition _condition;
        private System.Collections.Generic.List<Interfaces.IEffect> _effects;
        public override string Description { get; }
        public override bool IsOneTime { get; }
        public OnEnterLocationEvent(string description, Interfaces.ICondition condition,
            System.Collections.Generic.List<Interfaces.IEffect> effects, bool isOneTime = false)
        {
            Description = description; _condition = condition; _effects = effects; IsOneTime = isOneTime;
        }
        public override bool ShouldTrigger(Core.GameState state)
        {
            if (IsOneTime && IsTriggered) return false;
            return _condition.Check(state);
        }
        public override void Trigger(Core.GameState state)
        {
            for (int i = 0; i < _effects.Count; i++) { _effects[i].Apply(state); }
            IsTriggered = true;
        }
    }

    public class OnTurnEvent : Base.GameEventBase
    {
        private int _interval;
        private Interfaces.ICondition _condition;
        private System.Collections.Generic.List<Interfaces.IEffect> _effects;
        public override string Description { get; }
        public override bool IsOneTime => false;
        public OnTurnEvent(string description, int interval, Interfaces.ICondition condition,
            System.Collections.Generic.List<Interfaces.IEffect> effects)
        {
            Description = description; _interval = interval; _condition = condition; _effects = effects;
        }
        public override bool ShouldTrigger(Core.GameState state)
        {
            return state.Turn % _interval == 0 && _condition.Check(state);
        }
        public override void Trigger(Core.GameState state)
        {
            for (int i = 0; i < _effects.Count; i++) { _effects[i].Apply(state); }
        }
    }

    public class OneTimeEvent : Base.GameEventBase
    {
        private Interfaces.ICondition _condition;
        private System.Collections.Generic.List<Interfaces.IEffect> _effects;
        public override string Description { get; }
        public override bool IsOneTime => true;
        public OneTimeEvent(string description, Interfaces.ICondition condition,
            System.Collections.Generic.List<Interfaces.IEffect> effects)
        {
            Description = description; _condition = condition; _effects = effects;
        }
        public override bool ShouldTrigger(Core.GameState state)
        {
            if (IsTriggered) return false;
            return _condition.Check(state);
        }
        public override void Trigger(Core.GameState state)
        {
            for (int i = 0; i < _effects.Count; i++) { _effects[i].Apply(state); }
            IsTriggered = true;
        }
    }
}

// ============================================
// КВЕСТЫ
// ============================================
namespace QuestEngine.Quests
{
    public class GeneratorQuest : Base.QuestBase
    {
        public override string Id => "generator";
        public override string Title => "Включить генератор";
        public override string Description => "Найдите предметы и включите генератор.";
        public override void Update(Core.GameState state)
        {
            if (!IsCompleted && state.GetFlag("GeneratorOn"))
            {
                IsCompleted = true;
                state.LogMessage("КВЕСТ ВЫПОЛНЕН: Генератор включён!");
            }
        }
    }

    public class EscapeQuest : Base.QuestBase
    {
        public override string Id => "escape";
        public override string Title => "Побег из Сектора-7";
        public override string Description => "Доберитесь до выхода.";
        public override void Update(Core.GameState state)
        {
            if (!IsCompleted && state.CurrentLocation != null && state.CurrentLocation.Id == "Exit")
            {
                IsCompleted = true;
                state.LogMessage("КВЕСТ ВЫПОЛНЕН: Вы выбрались!");
                state.EndGame();
            }
        }
    }
}

// ============================================
// ПРОГРАММА (Main)
// ============================================
namespace QuestGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game();

            Location hall = new Location("Hall", "Холл", "Вы в главном холле комплекса.");
            Location storage = new Location("Storage", "Склад", "Здесь хранятся припасы.");
            Location corridor = new Location("DarkCorridor", "Тёмный коридор", "Очень темно.");
            Location generatorRoom = new Location("GeneratorRoom", "Комната генератора", "Генератор молчит.");
            Location exit = new Location("Exit", "Выход", "Свобода близко!");

            hall.AddExit("north", corridor);
            hall.AddExit("east", storage);
            storage.AddExit("west", hall);
            corridor.AddExit("south", hall);
            corridor.AddExit("north", generatorRoom);
            generatorRoom.AddExit("south", corridor);
            generatorRoom.AddExit("east", exit);
            exit.AddExit("west", generatorRoom);

            Chest chest1 = new Chest("chest1", "ящик", "Torch");
            Chest chest2 = new Chest("chest2", "сейф", "Key");
            Trap trap = new Trap("trap1", 20);

            System.Collections.Generic.List<IEffect> terminalEffects = new System.Collections.Generic.List<IEffect>();
            terminalEffects.Add(new SetFlagEffect("GeneratorOn", true));
            terminalEffects.Add(new LogEffect("Генератор запущен!"));
            NPC terminal = new NPC("terminal1", "Терминал", "Панель управления.", terminalEffects);

            storage.AddObject(chest1);
            storage.AddObject(trap);
            hall.AddObject(chest2);
            generatorRoom.AddObject(terminal);

            System.Collections.Generic.List<IEffect> darkEffects = new System.Collections.Generic.List<IEffect>();
            darkEffects.Add(new DamageEffect(10));
            darkEffects.Add(new LogEffect("В темноте вас ранило! -10 HP"));

            OnEnterLocationEvent darkEvent = new OnEnterLocationEvent(
                "Урон в темноте",
                new NotCondition(new HasItemCondition("Torch")),
                darkEffects,
                isOneTime: false
            );
            corridor.AddEvent(darkEvent);

            game.RegisterCommand(new HelpCommand());
            game.RegisterCommand(new LookCommand());
            game.RegisterCommand(new GoCommand());
            game.RegisterCommand(new InteractCommand());
            game.RegisterCommand(new InventoryCommand());
            game.RegisterCommand(new StatusCommand());

            game.RegisterQuest(new GeneratorQuest());
            game.RegisterQuest(new EscapeQuest());

            game.SetStartLocation(hall);
            game.Start();

            Console.WriteLine("=== ДОБРО ПОЖАЛОВАТЬ В СЕКТОР-7 ===");
            Console.WriteLine("Введите 'help' для списка команд.");
            Console.WriteLine();

            int lastLogCount = 0;

            while (game.IsRunning)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                
                if (string.IsNullOrEmpty(input))
                    continue;
                
                game.ProcessCommand(input);
                
                System.Collections.Generic.List<string> log = game.State.Log;
                for (int i = lastLogCount; i < log.Count; i++)
                {
                    Console.WriteLine(log[i]);
                }
                lastLogCount = log.Count;
            }

            Console.WriteLine();
            if (game.State.Health <= 0)
            {
                Console.WriteLine("ВЫ ПОГИБЛИ.");
            }
            else
            {
                Console.WriteLine("ПОЗДРАВЛЯЕМ! ВЫ ПРОШЛИ ИГРУ!");
            }
        }
    }
}
