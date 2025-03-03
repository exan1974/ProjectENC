using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace PatrolsManagement
{
    public class PatrolRoutes : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _positions = new List<GameObject>();
        [SerializeField] private List<string> _positionNames = new List<string>();

        // Random add position
        [SerializeField] private float _minPositionDistance = 2f;
        [SerializeField] private float _randomSpawnRadius = 200f;
        [SerializeField] private int _maxSearchAttempts = 100;

        // Patrol behavior
        [SerializeField] private bool _infiniteLoop = false;
        [SerializeField] private bool _randomTarget = false;
        [SerializeField] private bool _pingPong = false;
        [SerializeField] private int _loopCount = 1;
        [SerializeField] private float _waitTime = 0;
        private int _currentPositionListIndex = 0;
        private bool _isPingPongingForward = true;
        private int _currentLoop = 0;
        private bool _isPatrolling = false;
        private float _originalAgentSpeed;

        [SerializeField] private GameObject _positionContainer;
        private NavMeshAgent _agent;

        // Coroutine references
        private Coroutine _currentPatrolCoroutine = null;
        private Coroutine _currentRotationCoroutine = null;

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _originalAgentSpeed = _agent.speed;

            if (_agent == null)
            {
                Debug.LogError("PatrolRoutes: NavMeshAgent component not found on the NPC.");
                return;
            }

            if (_positions.Count > 0)
            {
                StartPatrol();
            }
        }

        private void Reset()
        {
            if (_positionContainer == null)
            {
                _positionContainer = new GameObject("Patrol positions of " + name);
            }
        }

        private void AddRandomPosition()
        {
            if (_positionContainer == null)
            {
                Reset();
            }

            int attempts = 0;
            Vector3 validPosition = Vector3.zero;
            bool positionFound = false;
            while (attempts < _maxSearchAttempts && !positionFound)
            {
                Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * _randomSpawnRadius;
                Vector3 potentialPosition = new Vector3(transform.position.x + randomPoint.x,
                    transform.position.y + 50f, transform.position.z + randomPoint.y);

                if (Physics.Raycast(potentialPosition, Vector3.down, out RaycastHit hitInfo, Mathf.Infinity))
                {
                    if (NavMesh.SamplePosition(hitInfo.point, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas))
                    {
                        Vector3 finalPosition = navHit.position;
                        bool tooClose = IsTooClose(finalPosition);

                        if (!tooClose)
                        {
                            validPosition = finalPosition;
                            positionFound = true;
                        }
                    }
                }

                attempts++;
            }

            if (positionFound)
            {
                CreatePositionData(validPosition);
            }
            else
            {
                Debug.LogWarning("No valid position found after maximum attempts.");
            }
        }

        private bool IsTooClose(Vector3 position)
        {
            if (_positions != null)
            {
                for (int i = 0; i < _positions.Count; i++)
                {
                    GameObject pos = _positions[i];

                    if (pos != null)
                    {
                        float distance = Vector3.Distance(pos.transform.position, position);
                        if (distance < _minPositionDistance)
                        {
                            return true;
                        }
                    }
                }
            }

            float npcDistance = Vector3.Distance(transform.position, position);

            if (npcDistance < _minPositionDistance)
            {
                return true;
            }

            return false;
        }

        private void AddPosition()
        {
            if (_positionContainer == null)
            {
                Reset();
            }

            CreatePositionData(transform.position);
        }

        private void RemovePosition(int index)
        {
            if (_positions != null && _positions.Any())
            {
                var go = _positions[index]; // or _positions.Last();

                if (go != null)
                {
                    DestroyImmediate(go);
                }

                _positions.RemoveAt(index);
            }

            if (_positionNames != null && _positionNames.Any())
            {
                _positionNames.RemoveAt(index);
            }
            
            if (_positions.Count == 0 && _positionContainer != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_positionContainer);
                }
                else
                {
                    DestroyImmediate(_positionContainer);
                }

                Debug.Log("PatrolRoutes: Removed the patrol positions container as there are no more patrol points.");
                _positionContainer = null;
            }
        }

        private void RemoveAllPositions()
        {
            if (_positions != null && _positions.Any())
            {
                for (int i = _positions.Count - 1; i >= 0; i--)
                {
                    RemovePosition(i);
                }
            }
        }

        private void CreatePositionData(Vector3 pos)
        {
            string positionName = "Set the name of position: " + _positions.Count;
            _positionNames.Add(positionName);
            var go = new GameObject("Position " + _positions.Count);
            _positions.Add(go);
            go.AddComponent<PositionPatrolGizmo>();
            go.transform.position = pos;
            go.transform.SetParent(_positionContainer.transform);
        }

        public void StartPatrol()
        {
            // Check if in play mode
            if (!Application.isPlaying)
            {
                return;
            }

            if (_positions == null || _positions.Count == 0)
            {
                Debug.LogWarning("PatrolRoutes: No patrol positions to patrol.");
                return;
            }

            if (_isPatrolling)
            {
                Debug.LogWarning("PatrolRoutes: Already patrolling.");
                return;
            }

            _isPatrolling = true;

            if (_infiniteLoop)
            {
                if (_randomTarget)
                {
                    _currentPatrolCoroutine = StartCoroutine(PatrolRandomlyInfinite());
                }
                else if (_pingPong)
                {
                    _currentPatrolCoroutine = StartCoroutine(PatrolPingPongInfinite());
                }
                else
                {
                    _currentPatrolCoroutine = StartCoroutine(PatrolInOrderInfinite());
                }
            }
            else
            {
                _currentPatrolCoroutine = StartCoroutine(PatrolInOrderLoopCount());
            }

            Debug.Log("PatrolRoutes: Patrol started.");
        }

        private IEnumerator PatrolInOrderInfinite()
        {
            while (_isPatrolling)
            {
                yield return MoveToPosition(_positions[_currentPositionListIndex].transform.position);

                _currentPositionListIndex = (_currentPositionListIndex + 1) % _positions.Count;
            }
        }

        private IEnumerator PatrolRandomlyInfinite()
        {
            // Initialize with an invalid index
            int lastIndex = -1;

            while (_isPatrolling)
            {
                int newIndex;
                do
                {
                    newIndex = UnityEngine.Random.Range(0, _positions.Count);
                } while (newIndex == lastIndex && _positions.Count > 1);

                lastIndex = newIndex;
                _currentPositionListIndex = newIndex;

                yield return MoveToPosition(_positions[_currentPositionListIndex].transform.position);
            }
        }

        private IEnumerator PatrolPingPongInfinite()
        {
            while (_isPatrolling)
            {
                yield return MoveToPosition(_positions[_currentPositionListIndex].transform.position);

                if (_isPingPongingForward)
                {
                    _currentPositionListIndex++;

                    if (_currentPositionListIndex >= _positions.Count)
                    {
                        _currentPositionListIndex = _positions.Count - 2;
                        _isPingPongingForward = false;
                    }
                }
                else
                {
                    _currentPositionListIndex--;

                    if (_currentPositionListIndex < 0)
                    {
                        _currentPositionListIndex = 1;
                        _isPingPongingForward = true;
                    }
                }
            }
        }

        private IEnumerator PatrolInOrderLoopCount()
        {
            for (_currentLoop = 0; _currentLoop < _loopCount && _isPatrolling; _currentLoop++)
            {
                for (_currentPositionListIndex = 0;
                     _currentPositionListIndex < _positions.Count && _isPatrolling;
                     _currentPositionListIndex++)
                {
                    yield return MoveToPosition(_positions[_currentPositionListIndex].transform.position);
                }
            }

            _isPatrolling = false;

            Debug.Log("PatrolRoutes: Completed all patrol loops.");
        }

        // Method derived from: https://discussions.unity.com/t/navmeshagent-does-not-wait-for-path-to-be-completed-before-moving-to-next-destination/855676
        private IEnumerator MoveToPosition(Vector3 targetPosition)
        {
            if (_agent == null)
            {
                Debug.LogError("PatrolRoutes: NavMeshAgent is not assigned.");
                yield break;
            }

            _agent.SetDestination(targetPosition);
            

            while (_agent.pathPending)
            {
                yield return null;
            }

            while (!_agent.pathPending && (_agent.remainingDistance > _agent.stoppingDistance || !_agent.hasPath))
            {
                yield return null;
            }

            if (_waitTime > 0)
            {
                yield return new WaitForSeconds(_waitTime);
            }
        }
        
        public void StopPatrol()
        {
            if (_isPatrolling)
            {
                _agent.speed = 0;

                Debug.Log("PatrolRoutes: Patrol stopped.");
            }
            else
            {
                Debug.LogWarning("PatrolRoutes: Attempted to stop patrol, but NPC is not patrolling.");
            }
        }

        public void ResumePatrol()
        {
            _agent.speed = _originalAgentSpeed;
        }
        
        public void FacePosition(Vector3 targetPosition)
        {
            if (_currentRotationCoroutine != null)
            {
                StopCoroutine(_currentRotationCoroutine);
            }

            _currentRotationCoroutine = StartCoroutine(RotateTowards(targetPosition));
        }
        
        private IEnumerator RotateTowards(Vector3 targetPosition)
        {
            float rotationSpeed = 5f; 
            
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0; 
            
            if (direction == Vector3.zero)
            {
                Debug.LogWarning("PatrolRoutes: Target position is the same as NPC position. Cannot rotate towards zero direction.");
                yield break;
            }
            
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                yield return null;
            }
            
            transform.rotation = targetRotation;
            
            _currentRotationCoroutine = null;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PatrolRoutes))]
        public class PatrolRoutesEditor : Editor
        {
            private SerializedProperty _positionsProp;
            private SerializedProperty _positionNamesProp;
            private SerializedProperty _infiniteLoopProp;
            private SerializedProperty _randomTargetProp;
            private SerializedProperty _pingPongProp;
            private SerializedProperty _loopCountProp;
            private SerializedProperty _waitTimeProp;

            private PatrolRoutes _patrolRoutes;

            private void OnEnable()
            {
                _positionsProp = serializedObject.FindProperty("_positions");
                _positionNamesProp = serializedObject.FindProperty("_positionNames");
                _infiniteLoopProp = serializedObject.FindProperty("_infiniteLoop");
                _randomTargetProp = serializedObject.FindProperty("_randomTarget");
                _pingPongProp = serializedObject.FindProperty("_pingPong");
                _loopCountProp = serializedObject.FindProperty("_loopCount");
                _waitTimeProp = serializedObject.FindProperty("_waitTime");
                _patrolRoutes = (PatrolRoutes)target;
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                GUILayout.Label("Patrol Positions Management", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Add a New Position"))
                {
                    _patrolRoutes.AddPosition();
                    serializedObject.Update();
                }

                if (GUILayout.Button("Add a Random New Position"))
                {
                    _patrolRoutes.AddRandomPosition();
                    serializedObject.Update();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                if (_positionsProp.arraySize > 0)
                {
                    if (GUILayout.Button("Remove Last Position"))
                    {
                        _patrolRoutes.RemovePosition(_positionsProp.arraySize - 1);
                        serializedObject.Update();
                    }

                    if (GUILayout.Button("Remove ALL Positions"))
                    {
                        _patrolRoutes.RemoveAllPositions();
                        serializedObject.Update();
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                if (_positionsProp.arraySize > 0)
                {
                    GUILayout.Label("Patrol Points:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box");
                    EditorGUIUtility.labelWidth = 200f;
                    
                    for (int i = 0; i < _positionsProp.arraySize; i++)
                    {
                        SerializedProperty positionProp = _positionsProp.GetArrayElementAtIndex(i);
                        SerializedProperty nameProp = _positionNamesProp.GetArrayElementAtIndex(i);

                        GameObject go = positionProp.objectReferenceValue as GameObject;

                        if (go == null)
                        {
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.BeginChangeCheck();
                        string newName = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.MinWidth(130));
                        if (EditorGUI.EndChangeCheck())
                        {
                            nameProp.stringValue = newName;
                            go.name = newName;
                        }

                        EditorGUI.BeginChangeCheck();
                        Vector3 newPos = EditorGUILayout.Vector3Field("", go.transform.position, GUILayout.Width(200));
                        if (EditorGUI.EndChangeCheck())
                        {
                            go.transform.position = newPos;
                        }

                        EditorGUILayout.EndHorizontal();

                        Rect separator = EditorGUILayout.GetControlRect(GUILayout.Height(1));
                        EditorGUI.DrawRect(separator, Color.gray);
                    }

                    EditorGUILayout.EndVertical();
                }

                GUILayout.Space(10);
                GUILayout.Label("Patrol Behavior Settings", EditorStyles.boldLabel);

                if (_positionsProp.arraySize < 2)
                {
                    GUILayout.Label("Add at least 2 patrol positions to set a behavior", EditorStyles.label);
                }
                else
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_infiniteLoopProp, new GUIContent("Infinite Loop"));

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();

                        if (!_infiniteLoopProp.boolValue)
                        {
                            _randomTargetProp.boolValue = false;
                            _pingPongProp.boolValue = false;
                            serializedObject.ApplyModifiedProperties();
                        }

                        _patrolRoutes.StartPatrol();
                    }

                    if (_infiniteLoopProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(_randomTargetProp, new GUIContent("Random Target"));

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (_randomTargetProp.boolValue)
                            {
                                _pingPongProp.boolValue = false;
                            }

                            serializedObject.ApplyModifiedProperties();
                            _patrolRoutes.StartPatrol();
                        }

                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(_pingPongProp, new GUIContent("Ping-Pong"));

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (_pingPongProp.boolValue)
                            {
                                _randomTargetProp.boolValue = false;
                            }

                            serializedObject.ApplyModifiedProperties();
                            _patrolRoutes.StartPatrol();
                        }

                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(_loopCountProp, new GUIContent("Loop Count"));

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (_loopCountProp.intValue < 1)
                            {
                                _loopCountProp.intValue = 1;
                            }

                            serializedObject.ApplyModifiedProperties();
                            _patrolRoutes.StartPatrol();
                        }

                        if (_loopCountProp.intValue < 1)
                        {
                            _loopCountProp.intValue = 1;
                            serializedObject.ApplyModifiedProperties();
                            _patrolRoutes.StartPatrol();
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_waitTimeProp, new GUIContent("Wait time at patrol point"));

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        _patrolRoutes.StartPatrol();
                    }
                    EditorGUILayout.EndVertical();
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}
