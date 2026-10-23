// The tests share one machine-wide setting (max processor state): a write test must not run while another
// test writes or restores it.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
