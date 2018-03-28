using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ObjectLogic
{
    #region Entity Base Clas
    // Base class for all game Entities, all EntityType classes (EntityPlayer, EntityItem, etc) inherit this
    public class Entity
    {
        protected static int currentMaxID = 1000000; // reserving first 1000000 IDs for pre-generated stuff and for clarity (1xxxxxx - not pregen, 0xxxxxx - pregen)

        // Enum for all types of Entities existing in game
        public enum EntityTypes
        {
            PLAYER, NPC, MONSTER,
            ITEM,
            INTERACTABLE_OBJECT, STATIC_OBJECT,
            QUEST
        };

        protected int thisID;
        protected EntityTypes thisType;

        // ---- CONSTRUCTOR. ID is optional

        public Entity(EntityTypes type, int id = -1)
        {
            int idToAdd = id;

            //Checking if provided ID is in acceptable bounds, modifying idToAdd if not

            if (id <= 0 || id > 1000000) idToAdd = currentMaxID;
            else idToAdd = id;

            //Checking if it's a duplicate for an Entity we already have. If not - adding as is
            if (!EntityContainer.Container.ContainsKey(thisID))
            {
                thisType = type;
                thisID = idToAdd;
                EntityContainer.Container.Add(thisID, this);
            }
            //If is a duplicate, adding as a new Entity with a new ID
            else
            {
                thisType = type;
                thisID = currentMaxID;
                EntityContainer.Container.Add(thisID, this);
            }

            //Remembering to move to unique ID every time we use constructor
            currentMaxID++;

        }

        // ---- DESTRUCTOR - not currently needed, no fields to manually dispose

        // ---- METHODS TO GET DATA. Allowing to access protected variables without exposing them

        // @@ RETURN - ID for Entity it's used on, of type int
        public int GetEntityID()
        {
            return this.thisID;
        }

        // @@ RETURN - type for Entity it's used on, of type Entity.EntityTypes
        public EntityTypes GetEntityType()
        {
            return this.thisType;
        }

        // @@ INPUT - int to assign, min and max int possible, value to assign if outside of boundaries
        // @@ RETURN - assigned value (int)

        public static int AssignValue (int toAssign, int min, int max, int safeSpot)
        {
            if (toAssign >= min && toAssign <= max) return toAssign;
            else return safeSpot;
        }

        // @@ INPUT - 2 ints to swap, passed by reference
        // @@ RETURN - nothing, all done in ref
        public static void SwapValues(ref int first, ref int second)
        {
            int temp = first; // Temp is less memory conservative, but helps to avoid overflow in (a = a-b, b = b+a, a = b-a)
            first = second;
            second = temp;
        }

    }
    #endregion

    #region Entity Container Class
    public class EntityContainer
    {

        // container for all entities, takes ID as key and Entity as value
        // **** REFACTOR - move to private IEnumerable instead to forbid Container.Add and Container.Remove in outer code
        public static Dictionary<int, Entity> Container = new Dictionary<int, Entity>();

        // ---- METHODS TO GET DATA

        //@@ INPUT - ID to find
        //@@ RETURN - reference to Entity with that ID or null if not found

        public static Entity GetReferenceToEntity(int id)
        {
            if (Container.ContainsKey(id)) return Container[id];
            else return null;
        }

    }
    #endregion

    #region Item Class
    public class EntityItem : Entity
    {
        // All possible item types. Each ItemType is a class (e.g. EntityItemWeapon) that inherits this with thisType set to proper variable in enum
        public enum ItemTypes
        {
            WEAPON, SHIELD, ARMOR_HEAD, ARMOR_BODY, ARMOR_LEGS, ARMOR_HANDS, RING, BRACELET, AMULET,
            QUEST_OBJECT, QUEST_WEAPON,
            POTION, MAGIC_CONSUMABLE, SCROLL,
            GOLD,
            RESOURCE_METAL, RESOURCE_HERB, RESOURCE_STONE
        };

        protected ItemTypes thisItemType;

        // ---- CONSTRUCTOR. Takes a type of ItemTypes and an id to generate a new Entity as well. Uses base Entity constructor.

        public EntityItem (ItemTypes type, int id = -1) : base (EntityTypes.ITEM, id) 
        {
            thisItemType = type;
        }

        // ---- METHODS TO GET DATA

        //@@ RETURN - type of item it's used on, of ItemTypes

        public ItemTypes GetItemType()
        {
            return thisItemType;
        }

    }
    #endregion

    #region Weapon Class
    public class EntityItemWeapon : EntityItem
    {
        //Types of weapons, used in generation to determine fields (min-max range, if uses consumables, if crits, if negates armor)
        public enum WeaponTypes {
            SWORD_STRAIGHT_LONG, SWORD_STRAIGHT_SHORT, SWORD_CURVED,
            BOW_LONG, BOW_SHORT,
            CROSSBOW_LIGHT, CROSSBOW_HEAVY,
            AXE,
            BARE_HANDS }

        protected WeaponTypes thisWeaponType;

        // Declaration of weapon stats
        // RANGE
        protected int minRange = 0;
        protected int maxRange = 0;
        // DAMAGE
        protected int minDamage = 0;
        protected int maxDamage = 0;
        // CRIT - both lated divided by 100 in damage formula. critChancePercent = 0 -- never crit, critMultiplierPercent = 100 -- crit damage is multiplied by 1
        protected int critChancePercent = 0;
        protected int critMupliplierPercent = 100;
        // REQUIREMENTS
        // **** REFACTOR - move to Stats class
        protected int levelRequired = 0;
        protected int skillRequired = 0;

        // ---- CONSTRUCTOR. Takes all parameters needed to create a weapon, chains generation of EntityItem and Entity. If min and max are in wrong order, they're auto swapped

        public EntityItemWeapon(int id, EntityItemWeapon.WeaponTypes type, int _minRange, int _maxRange, int _minDamage, int _maxDamage,
                                int _critChancePercent, int _critMultiplierPercent, int _levelRequired, int _skillRequired) : base(EntityItem.ItemTypes.WEAPON, id)
        {

            thisWeaponType = type;

            // 50 is current max range for every weapon, **** REFACTOR - change to global variable available in editor
            minRange = Entity.AssignValue(_minRange, 0, 50, 0);
            maxRange = Entity.AssignValue(_maxRange, 0, 50, 2); // 2 is a range of bare fists, using that as safe spot

            if (minRange > maxRange) Entity.SwapValues(ref minRange, ref maxRange);

            minDamage = Entity.AssignValue(_minDamage, 0, 1000000, 0);
            maxDamage = Entity.AssignValue(_maxDamage, 0, 1000000, 1); // 1 is damage of bare fists, using that as safe spot

            if (minDamage > maxDamage) Entity.SwapValues(ref minDamage, ref maxDamage);

            critChancePercent = Entity.AssignValue(_critChancePercent, 0, 100, 0);
            critMupliplierPercent = Entity.AssignValue(_critMultiplierPercent, 0, 1000000, 100); // temporarily allowing for crits to be 0x..10000x of damage

            levelRequired = Entity.AssignValue(_levelRequired, 0, 200, 0); // 50 is current level cap, item generator checks that item can't be over level cap
            skillRequired = Entity.AssignValue(_skillRequired, 0, 1000, 0); // 100 is current skill cape, generator checks that item won't be over cap

        }
    }
    #endregion

    public class ObjectLogic : MonoBehaviour
    {
    }
}