﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#pragma warning disable 1591

// Note: Please be careful when modifying this because it could break existing components!

namespace LiveSplit.ComponentUtil
{
    public class MemoryWatcherList : List<MemoryWatcher>
    {
        public delegate void MemoryWatcherDataChangedEventHandler(MemoryWatcher watcher);
        public event MemoryWatcherDataChangedEventHandler OnWatcherDataChanged;

        public MemoryWatcher this[string name]
        {
            get { return this.First(w => w.Name == name); }
        }

        public void UpdateAll(Process process)
        {
            if (OnWatcherDataChanged != null)
            {
                var changedList = new List<MemoryWatcher>();

                foreach (var watcher in this)
                {
                    bool changed = watcher.Update(process);
                    if (changed)
                        changedList.Add(watcher);
                }

                // only report changes when all of the other watches are updated too
                foreach (var watcher in changedList)
                {
                    OnWatcherDataChanged(watcher);
                }
            }
            else
            {
                foreach (var watcher in this)
                {
                    watcher.Update(process);
                }
            }
        }

        public void ResetAll()
        {
            foreach (var watcher in this)
            {
                watcher.Reset();
            }
        }
    }

    public abstract class MemoryWatcher
    {
        public enum ReadFailAction
        {
            DontUpdate,
            SetZeroOrNull,
        }

        public string Name { get; set; }
        public bool Enabled { get; set; }
        public object Current { get; set; }
        public object Old { get; set; }
        public bool Changed { get; protected set; }
        public TimeSpan? UpdateInterval { get; set; }
        public ReadFailAction FailAction { get; set; }

        protected bool InitialUpdate { get; set; }
        protected DateTime? LastUpdateTime { get; set; }
        protected IntPtr Address { get; set; }

        protected AddressType AddrType { get; }
        protected enum AddressType { DeepPointer, Absolute }

        protected MemoryWatcher(IntPtr address)
        {
            Address = address;
            AddrType = AddressType.Absolute;
            Enabled = true;
            FailAction = ReadFailAction.DontUpdate;
        }

        /// <summary>
        /// Updates the watcher and returns true if the value has changed.
        /// </summary>
        public abstract bool Update(Process process);
            
        public abstract void Reset();

        protected bool CheckInterval()
        {
            if (UpdateInterval.HasValue)
            {
                if (LastUpdateTime.HasValue)
                {
                    if (DateTime.Now - LastUpdateTime.Value < UpdateInterval.Value)
                        return false;
                }
                LastUpdateTime = DateTime.Now;
            }
            return true;
        }
    }

    public class MemoryWatcher<T> : MemoryWatcher where T : struct
    {
        public new T Current
        {
            get { return (T)(base.Current ?? default(T)); }
            set { base.Current = value; }
        }
        public new T Old
        {
            get { return (T)(base.Old ?? default(T)); }
            set { base.Old = value; }
        }

        public delegate void DataChangedEventHandler(T old, T current);
        public event DataChangedEventHandler OnChanged;
        
        public MemoryWatcher(IntPtr address)
            : base(address) { }

        public override bool Update(Process process)
        {
            Changed = false;

            if (!Enabled)
                return false;

            if (!CheckInterval())
                return false;

            base.Old = Current;

            T val;
            bool success;
            
            success = process.ReadValue(Address, out val);

            if (success)
            {
                base.Old = base.Current;
                base.Current = val;
            }
            else
            {
                if (FailAction == ReadFailAction.DontUpdate)
                    return false;

                base.Old = base.Current;
                base.Current = val;
            }

            if (!InitialUpdate)
            {
                InitialUpdate = true;
                return false;
            }

            if (!Current.Equals(Old))
            {
                OnChanged?.Invoke(Old, Current);
                Changed = true;
                return true;
            }

            return false;
        }

        public override void Reset()
        {
            base.Current = default(T);
            base.Old = default(T);
            InitialUpdate = false;
        }
    }
}
