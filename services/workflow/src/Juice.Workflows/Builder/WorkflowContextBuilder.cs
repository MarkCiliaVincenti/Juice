﻿using System.Collections.Concurrent;
using Juice.Services;

namespace Juice.Workflows.Builder
{
    // Build the simple workflow for logic testing

    public abstract class WorkflowContextBuilderBase
    {
        protected Dictionary<string, NodeRecord> _nodeRecords = new Dictionary<string, NodeRecord>();
        protected Dictionary<string, INode> _nodes = new Dictionary<string, INode>();
        protected Dictionary<string, FlowRecord> _flowRecords = new Dictionary<string, FlowRecord>();
        protected Dictionary<string, IFlow> _flows = new Dictionary<string, IFlow>();

        protected Dictionary<string, ProcessRecord> _processRecords = new Dictionary<string, ProcessRecord>();

        protected IStringIdGenerator _idGenerator;
        protected INodeLibrary _nodeLibrary;
        protected IServiceProvider _serviceProvider;
        protected bool _needBuild = true;
        public WorkflowContextBuilderBase(
           IStringIdGenerator stringIdGenerator,
           INodeLibrary nodeLibrary,
           IServiceProvider serviceProvider
       )
        {
            _idGenerator = stringIdGenerator;
            _nodeLibrary = nodeLibrary;
            _serviceProvider = serviceProvider;
        }


        #region Builder
        protected string NewProcessId() => "Process_" + _idGenerator.GenerateRandomId(6);
        protected string NewEventId() => "Event_" + _idGenerator.GenerateRandomId(6);
        protected string NewGatewayId() => "Gateway_" + _idGenerator.GenerateRandomId(6);
        protected string NewActivityId() => "Activity_" + _idGenerator.GenerateRandomId(6);
        protected string NewFlowId() => "Flow_" + _idGenerator.GenerateRandomId(6);

        protected virtual void AddNode(NodeRecord record, INode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (_nodes.ContainsKey(record.Id))
            {
                throw new ArgumentException($"Node {record.Id} is already exists.");
            }
            _nodes[record.Id] = node;
            _nodeRecords[record.Id] = record;
        }

        protected virtual void AddFlow(FlowRecord record, IFlow flow, bool isDefault = false)
        {
            if (flow == null)
            {
                throw new ArgumentNullException(nameof(flow));
            }
            if (_flows.ContainsKey(record.Id))
            {
                throw new ArgumentException($"Flow {record.Id} is already exists.");
            }

            if (!_nodeRecords.ContainsKey(record.SourceRef))
            {
                throw new ArgumentException($"Source node {record.SourceRef} does not exist.");
            }

            if (!_nodeRecords.ContainsKey(record.DestinationRef))
            {
                throw new ArgumentException($"Destination node {record.DestinationRef} does not exist.");
            }

            _flows[record.Id] = flow;
            _flowRecords[record.Id] = record;

            var source = _nodeRecords[record.SourceRef];
            source.AddOutgoing(record.Id);

            if (isDefault)
            {
                source.SetDefault(record.Id);
            }

            var dest = _nodeRecords[record.DestinationRef];
            dest.AddIncoming(record.Id);
        }

        protected (INode Node, NodeRecord Record) CreateNode(string type, string? name, string processId)
        {
            var node = _nodeLibrary.CreateInstance(type, _serviceProvider);
            var nodeId = node is IGateway ? NewGatewayId()
            : node is IEvent ? NewEventId()
            : NewActivityId();
            var record = new NodeRecord { Id = nodeId, Name = name ?? node.DisplayText, ProcessIdRef = processId };
            return (node, record);
        }

        protected virtual void AddProcess(ProcessRecord process)
        {
            _processRecords[process.Id] = process;
        }
        #endregion
    }

    public class WorkflowContextBuilder : WorkflowContextBuilderBase
    {
        public WorkflowContextBuilder(IStringIdGenerator stringIdGenerator,
            INodeLibrary nodeLibrary,
            IServiceProvider serviceProvider) : base(stringIdGenerator, nodeLibrary, serviceProvider)
        {

        }

        public WorkflowContext Build(WorkflowRecord workflow,
            WorkflowState? state,
            string? user,
            Dictionary<string, object?>? input)
        {

            return new WorkflowContext(workflow.Id
                , workflow.CorrelationId
                , state?.NodeSnapshots
                , state?.FlowSnapshots
                , input
                , state?.Output
                , _nodeRecords.Values.Select(n => new NodeContext(n, _nodes[n.Id])).ToList()
                , _flowRecords.Values.Select(f => new FlowContext(f, _flows[f.Id])).ToList()
                , _processRecords.Values
                , workflow.Name
                , user
                );
        }


        protected override void AddNode(NodeRecord record, INode node)
        {
            base.AddNode(record, node);
            _currentNodeId = record.Id;
        }

        #region Workflow as code

        private string? _currentNodeId;
        private string? _currentProcessId;

        private WorkflowContextBuilder Append<T>(string? name = default, string? condition = default, bool isDefault = false)
    where T : class, INode
        {
            if (string.IsNullOrEmpty(_currentNodeId))
            {
                throw new InvalidOperationException("Workflow must has current node before");
            }

            if (typeof(T).IsAssignableTo(typeof(StartEvent)))
            {
                throw new ArgumentException("Start Event must be start of workflow");
            }

            var current = _nodes[_currentNodeId];
            if (current.GetType().IsAssignableTo(typeof(IGateway)))
            {
                if (!_gateways.TryPeek(out var gid) || gid != _currentNodeId)
                {
                    _gateways.Push(_currentNodeId);
                }
                if (current.GetType().IsAssignableTo(typeof(IEventBased))
                    && (
                        !typeof(T).IsAssignableTo(typeof(ICatching))
                        || !typeof(T).IsAssignableTo(typeof(IIntermediate))
                        )
                    )
                {
                    throw new ArgumentException("The next to Eventbased gateway must be intermediate catch event");
                }
            }
            else if (current.GetType().IsAssignableTo(typeof(IActivity)))
            {
                _branchActivities.Push(_currentNodeId);
            }

            var currentId = _currentNodeId;

            var id = typeof(T).IsAssignableTo(typeof(IGateway)) ? NewGatewayId()
                : typeof(T).IsAssignableTo(typeof(IEvent)) ? NewEventId()
                : NewActivityId();
            var flowId = NewFlowId();

            var node = _nodeLibrary.CreateInstance(typeof(T).Name, _serviceProvider);
            var record = new NodeRecord(id, name ?? node.DisplayText, _currentProcessId);

            AddNode(record, node);

            var flow = new SequenceFlow { };
            var flowRecord = new FlowRecord(flowId, currentId, id, default, _currentProcessId, condition);

            AddFlow(flowRecord, flow, isDefault);

            return this;
        }

        public WorkflowContextBuilder NewProcess(string? processId = default, string? name = default)
        {
            var process = new ProcessRecord(processId ?? NewProcessId(), name);
            _processRecords[process.Id] = process;
            _currentProcessId = process.Id;
            return this;
        }
        public WorkflowContextBuilder Start()
        {
            if (_currentProcessId == null)
            {
                NewProcess();
            }
            _currentNodeId = NewEventId();
            var record = new NodeRecord(_currentNodeId, "Start", _currentProcessId);
            AddNode(record, _nodeLibrary.CreateInstance(nameof(StartEvent), _serviceProvider));
            return this;
        }
        public WorkflowContextBuilder End()
            => Append<EndEvent>();
        public WorkflowContextBuilder Terminate()
           => Append<TerminateEvent>();

        public WorkflowContextBuilder Then<T>(string? name = default, string? condition = default, bool isDefault = false)
            where T : class, IActivity
            => Append<T>(name, condition, isDefault);

        public WorkflowContextBuilder Wait<T>(string? name = default, string? condition = default, bool isDefault = false)
            where T : class, IIntermediate
            => Append<T>(name, condition, isDefault);

        public WorkflowContextBuilder Attach<T>(string name)
            where T : class, IBoundary
        {
            if (string.IsNullOrEmpty(_currentNodeId))
            {
                throw new InvalidOperationException("Workflow must has current node before");
            }
            var id = NewEventId();
            var node = _nodeLibrary.CreateInstance(typeof(T).Name, _serviceProvider);
            var record = new NodeRecord(id, name ?? node.DisplayText).AttachTo(_currentNodeId);

            AddNode(record, node);

            return this;
        }

        private Stack<string> _branchActivities = new Stack<string>();
        private Stack<string> _gateways = new Stack<string>();

        public WorkflowContextBuilder Exclusive(string? name = default)
            => Gateway<ExclusiveGateway>(name);

        public WorkflowContextBuilder ExclusiveEventbased(string? name = default)
            => Gateway<EventBasedGateway>(name);

        public WorkflowContextBuilder Inclusive(string? name = default)
            => Gateway<InclusiveGateway>(name);

        public WorkflowContextBuilder Parallel(string? name = default)
            => Gateway<ParallelGateway>(name);

        private WorkflowContextBuilder Gateway<T>(string? name)
            where T : class, IGateway
        {
            if (string.IsNullOrEmpty(_currentNodeId))
            {
                throw new InvalidOperationException("Workflow must has current node before");
            }
            var current = _nodes[_currentNodeId];
            if (current.GetType().IsAssignableTo(typeof(IGateway)))
            {
                throw new InvalidOperationException("Gateway must has activity or event next to");
            }
            if (!string.IsNullOrEmpty(name) && _nodeRecords.Values.Any(n => n.Name == name))
            {
                throw new Exception($"Name {name} is existing");
            }
            Append<T>(name);
            _branchActivities.Clear();
            return this;
        }
        private WorkflowContextBuilder LastGateway()
        {
            if (_gateways.TryPop(out var id))
            {
                _currentNodeId = id;
                return this;
            }
            throw new InvalidOperationException("No more gateway to go back");
        }

        public WorkflowContextBuilder Fork()
        {

            if (string.IsNullOrEmpty(_currentNodeId))
            {
                throw new InvalidOperationException("Workflow must has current node before");
            }

            var current = _nodes[_currentNodeId];

            if (!current.GetType().IsAssignableTo(typeof(IGateway)))
            {
                if (!_gateways.Any())
                {
                    throw new InvalidOperationException("Could not fork because no ancestor gateway found");
                }
                return LastGateway();
            }
            return this;
        }

        public WorkflowContextBuilder Merge(params string[] names)
        {
            if (string.IsNullOrEmpty(_currentNodeId))
            {
                throw new InvalidOperationException("Workflow must has current node before");
            }

            var current = _nodes[_currentNodeId];

            if (!current.GetType().IsAssignableTo(typeof(IGateway)))
            {
                if (!_gateways.Any())
                {
                    throw new InvalidOperationException("Could not fork because no ancestor gateway found");
                }
                LastGateway();
            }

            var newCurrent = _nodes[_currentNodeId];
            var currentGatewayId = new string(_currentNodeId);

            var gatewayType = newCurrent.GetType();

            var leafNodes = names.Any() ? _nodeRecords.Values.Where(n => names.Contains(n.Name)).Select(n => n.Id)
                : GetLeafNodes(_nodeRecords[_currentNodeId]).Distinct();

            var id = NewGatewayId();
            var record = new NodeRecord { Id = id, Name = "Merge" };
            var mergeGateway = _nodeLibrary.CreateInstance(gatewayType.Name, _serviceProvider);
            AddNode(record, mergeGateway);

            foreach (var leaf in leafNodes)
            {
                var flowId = NewFlowId();
                var flowRecord = new FlowRecord(flowId, leaf, id);
                var flow = new SequenceFlow();
                AddFlow(flowRecord, flow);
            }

            if (current.GetType().IsAssignableTo(typeof(IGateway)))
            {
                var flowId = NewFlowId();
                var flowRecord = new FlowRecord(flowId, currentGatewayId, id);

                var flow = new SequenceFlow();
                AddFlow(flowRecord, flow, true);
            }

            return this;
        }

        public WorkflowContextBuilder Seek(string name)
        {
            _currentNodeId = _nodeRecords.Values.First(n => n.Name == name).Id;
            return this;
        }

        public WorkflowContextBuilder SubProcess(string name,
            Action<WorkflowContextBuilder> builder,
            string? condition,
            bool isDefault = false)
        {
            if (string.IsNullOrEmpty(_currentNodeId))
            {
                throw new InvalidOperationException("Workflow must has current node before");
            }

            var currentId = _currentNodeId;
            var id = NewActivityId();
            var flowId = NewFlowId();

            var node = _nodeLibrary.CreateInstance(typeof(SubProcess).Name, _serviceProvider);
            var record = new NodeRecord { Id = id, Name = name ?? node.DisplayText };

            AddNode(record, node);

            var flow = new SequenceFlow { };
            var flowRecord = new FlowRecord(flowId, currentId, id, default, default, condition);

            AddFlow(flowRecord, flow, isDefault);

            var tmpWorkflowId = _idGenerator.GenerateRandomId(6);

            var contextBuilder = new WorkflowContextBuilder(_idGenerator, _nodeLibrary, _serviceProvider)
                .NewProcess(record.Id, record.Name);
            builder(contextBuilder);

            string? nullCorrelationId = default;
            string? nullName = default;
            string? nullUser = default;
            WorkflowState? nullState = default;
            Dictionary<string, object?>? nullInput = default;

            var context = contextBuilder.Build(new WorkflowRecord(tmpWorkflowId, tmpWorkflowId, nullCorrelationId, nullName),
                nullState, nullUser, nullInput);

            foreach (var n in context.Nodes.Values)
            {
                _nodes[n.Record.Id] = n.Node;
                _nodeRecords[n.Record.Id] = n.Record;
            }

            foreach (var f in context.Flows)
            {
                _flows[f.Record.Id] = f.Flow;
                _flowRecords[f.Record.Id] = f.Record;
            }
            return this;
        }

        private List<string> GetLeafNodes(string flowId)
        {
            var flow = _flowRecords[flowId];
            var destNode = _nodeRecords[flow.DestinationRef];
            return GetLeafNodes(destNode);
        }

        private List<string> GetLeafNodes(NodeRecord node)
        {
            var leafNodes = new List<string>();
            var branches = node.Outgoings;
            if (branches.Any())
            {
                foreach (var branch in branches)
                {
                    leafNodes.AddRange(GetLeafNodes(branch));
                }
            }
            else
            {
                leafNodes.Add(node.Id);
            }
            return leafNodes;
        }
        #endregion

    }

    // Manage static incode workflow
    internal class IncodeWorkflowContextBuilder : IWorkflowContextBuilder
    {
        private static ConcurrentDictionary<string, WorkflowContextBuilder> _store = new ConcurrentDictionary<string, WorkflowContextBuilder>();

        private IWorkflowStateReposistory _stateReposistory;
        public IncodeWorkflowContextBuilder(IWorkflowStateReposistory stateReposistory)
        {
            _stateReposistory = stateReposistory;
        }

        public async Task<WorkflowContext> BuildAsync(string workflowId,
            string instanceId, Dictionary<string, object?>? input,
            CancellationToken token)
        {
            var state = await _stateReposistory.GetAsync(instanceId, token);

            string? nullUser = default;

            return _store[workflowId].Build(new WorkflowRecord(instanceId, workflowId, default, default),
                state, nullUser, input);
        }
        public Task<bool> ExistsAsync(string workflowId, CancellationToken token)
            => Task.FromResult(_store.ContainsKey(workflowId));


        public void Register(string workflowId, WorkflowContextBuilder builder)
        {
            _store[workflowId] = builder;
        }
    }
}
