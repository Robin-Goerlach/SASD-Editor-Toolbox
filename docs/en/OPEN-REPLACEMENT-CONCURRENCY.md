# Replacement open: asynchronous interleaving tests

The replacement-document service is now regression-tested against controlled
asynchronous codec completions. The tests suspend the decoder without sleeps,
edit the original document, close the initiating window, switch the active
window, change buffer content without incrementing the version, or cancel
the operation. Only then do they release decoding.

Important contracts:

- A new edit or a changed source snapshot blocks stale replacement.
- A closed initiating window cannot be reintroduced.
- Changing keyboard focus does not redirect the open into another window.
- External cancellation is rechecked immediately before reattachment.
- Rejecting a stale operation leaves existing text and view identities intact.

This milestone intentionally leaves the work in the reusable editor Core,
rather than faking concurrency in console code. The newly documented
last-chance cancellation gate complements existing validation. Terminal
acceptance and adversarial filesystem race tests remain separate work.
